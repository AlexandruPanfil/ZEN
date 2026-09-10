using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZEN.Application.Services.Interfaces;
using ZEN.Application.DTOs.Sync;
using ZEN.Domain.Entities;
using ZEN.Infrastructure.Persistence;

namespace ZEN.Windows.Services.Sync;

public class PosSyncService : IPosSyncService
{
    private readonly ZenDbContext _dbContext;
    private readonly SyncApiClient _syncApiClient;
    private readonly ILogger<PosSyncService> _logger;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public PosSyncService(ZenDbContext dbContext, SyncApiClient syncApiClient, ILogger<PosSyncService> logger)
    {
        _dbContext = dbContext;
        _syncApiClient = syncApiClient;
        _logger = logger;
    }

    public async Task RunSyncAsync(CancellationToken cancellationToken = default)
    {
        if (!await _syncLock.WaitAsync(0, cancellationToken))
        {
            _logger.LogInformation("Sync skipped because another sync run is already in progress.");
            return;
        }

        try
        {
            var syncState = await GetOrCreateSyncStateAsync(cancellationToken);

            await PushPendingPaymentsAsync(syncState, cancellationToken);
            await PushPendingPrintersAsync(cancellationToken);
            await PullProductsAsync(syncState, cancellationToken);

            syncState.LastSuccessfulSyncUtc = DateTime.UtcNow;
            syncState.LastError = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        catch (Exception ex)
        {
            var syncState = await GetOrCreateSyncStateAsync(cancellationToken);
            syncState.LastError = ex.Message;
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, "POS sync run failed.");
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private async Task PushPendingPrintersAsync(CancellationToken cancellationToken)
    {
        var pending = await _dbContext.SyncOutboxItems
            .Where(x => x.EntityType.StartsWith("Printer:") && x.Status == "Pending")
            .OrderBy(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var outbox in pending)
        {
            outbox.Status = "InProgress";
            outbox.LastAttemptUtc = DateTime.UtcNow;
            outbox.RetryCount++;
            await _dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                var succeeded = outbox.EntityType switch
                {
                    "Printer:Create" => await PushPrinterCreateAsync(outbox, cancellationToken),
                    "Printer:Update" => await PushPrinterUpdateAsync(outbox, cancellationToken),
                    "Printer:Delete" => await _syncApiClient.DeletePrinterAsync(outbox.EntityId, cancellationToken),
                    _ => throw new InvalidOperationException($"Unknown printer sync operation '{outbox.EntityType}'.")
                };

                outbox.Status = succeeded ? "Synced" : "Pending";
                outbox.Error = succeeded ? null : "Server rejected printer synchronization.";
            }
            catch (HttpRequestException ex)
            {
                outbox.Status = "Pending";
                outbox.Error = ex.Message;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                outbox.Status = "Pending";
                outbox.Error = "Printer synchronization timed out.";
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> PushPrinterCreateAsync(
        SyncOutboxItem outbox,
        CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<CreatePrinterRequest>(outbox.PayloadJson)
            ?? throw new InvalidOperationException("Invalid printer create payload.");
        return await _syncApiClient.CreatePrinterAsync(request, cancellationToken) is not null;
    }

    private async Task<bool> PushPrinterUpdateAsync(
        SyncOutboxItem outbox,
        CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<UpdatePrinterRequest>(outbox.PayloadJson)
            ?? throw new InvalidOperationException("Invalid printer update payload.");
        return await _syncApiClient.UpdatePrinterAsync(request, cancellationToken) is not null;
    }

    private async Task PushPendingPaymentsAsync(SyncState syncState, CancellationToken cancellationToken)
    {
        var pending = await _dbContext.SyncOutboxItems
            .Where(x => x.EntityType == "Payment" && x.Status == "Pending")
            .OrderBy(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        var items = new List<PaymentSyncItemDto>(pending.Count);

        foreach (var outbox in pending)
        {
            var dto = JsonSerializer.Deserialize<PaymentSyncItemDto>(outbox.PayloadJson);
            if (dto is null)
            {
                outbox.Status = "Failed";
                outbox.Error = "Invalid payload JSON";
                continue;
            }

            items.Add(dto);
            outbox.Status = "InProgress";
            outbox.LastAttemptUtc = DateTime.UtcNow;
            outbox.RetryCount += 1;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (items.Count == 0)
        {
            return;
        }

        var request = new UploadPaymentsRequest(Environment.MachineName, items);
        var response = await _syncApiClient.UploadPaymentsAsync(request, cancellationToken);

        if (response is null)
        {
            foreach (var outbox in pending.Where(p => p.Status == "InProgress"))
            {
                outbox.Status = "Pending";
                outbox.Error = "Upload request failed";
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var resultMap = response.Results.ToDictionary(r => r.PaymentId, r => r);

        foreach (var outbox in pending.Where(p => p.Status == "InProgress"))
        {
            if (!resultMap.TryGetValue(outbox.EntityId, out var result))
            {
                outbox.Status = "Pending";
                outbox.Error = "Missing result from server";
                continue;
            }

            if (result.Status is "Accepted" or "Duplicate")
            {
                outbox.Status = "Synced";
                outbox.Error = null;
            }
            else
            {
                outbox.Status = "Failed";
                outbox.Error = result.Message;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task PullProductsAsync(SyncState syncState, CancellationToken cancellationToken)
    {
        var response = await _syncApiClient.GetProductsDeltaAsync(syncState.LastProductsSyncUtc, cancellationToken);
        if (response is null)
        {
            return;
        }

        foreach (var item in response.Items)
        {
            var existing = await _dbContext.Products
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == item.Id, cancellationToken);

            if (existing is null)
            {
                existing = new Product
                {
                    Id = item.Id
                };

                await _dbContext.Products.AddAsync(existing, cancellationToken);
            }

            existing.Name = item.Name;
            existing.Barcode = item.Barcode;
            existing.SKU = item.SKU;
            existing.CostPrice = item.CostPrice;
            existing.SalePrice = item.SalePrice;
            existing.IsActive = item.IsActive;
            existing.IsDeleted = item.IsDeleted;
            existing.UpdatedAt = item.UpdatedAt;
            existing.TaxId = item.TaxId;
        }

        syncState.LastProductsSyncUtc = response.ServerTimeUtc;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<SyncState> GetOrCreateSyncStateAsync(CancellationToken cancellationToken)
    {
        var syncState = await _dbContext.SyncStates
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (syncState is not null)
        {
            return syncState;
        }

        syncState = new SyncState();
        await _dbContext.SyncStates.AddAsync(syncState, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return syncState;
    }
}