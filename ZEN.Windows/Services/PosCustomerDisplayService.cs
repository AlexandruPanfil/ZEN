using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZEN.Application.DTOs.Admin;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Infrastructure.Persistence;
using ZEN.Windows.Services.Sync;

namespace ZEN.Windows.Services;

public sealed class PosCustomerDisplayService
{
    private readonly ZenDbContext _dbContext;
    private readonly ICustomerDisplayService _localService;
    private readonly SyncApiClient _apiClient;

    public PosCustomerDisplayService(
        ZenDbContext dbContext,
        ICustomerDisplayService localService,
        SyncApiClient apiClient)
    {
        _dbContext = dbContext;
        _localService = localService;
        _apiClient = apiClient;
    }

    public Task<IEnumerable<CustomerDisplayDto>> GetCachedAsync(CancellationToken cancellationToken = default) =>
        _localService.GetAllAsync(cancellationToken);

    public async Task<IReadOnlyList<CustomerDisplayDto>> RefreshAsync(
        Guid? storeId,
        CancellationToken cancellationToken = default)
    {
        var displays = (await _apiClient.GetCustomerDisplaysAsync(storeId, cancellationToken)).ToList();
        var pendingEntityIds = await _dbContext.SyncOutboxItems
            .Where(x => x.EntityType.StartsWith("CustomerDisplay:")
                && (x.Status == "Pending" || x.Status == "InProgress"))
            .Select(x => x.EntityId)
            .ToHashSetAsync(cancellationToken);
        var pendingDeleteIds = await _dbContext.SyncOutboxItems
            .Where(x => x.EntityType == "CustomerDisplay:Delete"
                && (x.Status == "Pending" || x.Status == "InProgress"))
            .Select(x => x.EntityId)
            .ToHashSetAsync(cancellationToken);
        displays = displays
            .Where(x => !pendingDeleteIds.Contains(x.Id))
            .ToList();
        var serverEntityIds = displays.Select(x => x.Id).ToHashSet();
        foreach (var dto in displays)
        {
            if (pendingEntityIds.Contains(dto.Id))
            {
                continue;
            }
            var existing = await _dbContext.CustomerDisplays
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.Id == dto.Id, cancellationToken);

            if (existing is null)
            {
                existing = new CustomerDisplay { Id = dto.Id };
                await _dbContext.CustomerDisplays.AddAsync(existing, cancellationToken);
            }

            existing.Name = dto.Name;
            existing.ConnectionType = dto.ConnectionType;
            existing.ConnectionString = dto.ConnectionString;
            existing.Size = dto.Size;
            existing.CashDeskId = dto.CashDeskId;
            existing.IsActive = dto.IsActive;
            existing.Version = dto.Version;
            existing.IsDeleted = dto.IsDeleted;
        }

        var missingLocalDisplays = await _dbContext.CustomerDisplays
            .IgnoreQueryFilters()
            .Where(x => !pendingEntityIds.Contains(x.Id) && !serverEntityIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
        foreach (var display in missingLocalDisplays)
        {
            display.IsDeleted = true;
        }

        await _dbContext.SaveServerSynchronizationAsync(cancellationToken);
        return displays.Where(x => !x.IsDeleted).ToList();
    }

    public async Task<CustomerDisplayDto> CreateAsync(
        CreateCustomerDisplayRequest request,
        CancellationToken cancellationToken = default)
    {
        var localRequest = request with { Id = request.Id ?? Guid.NewGuid() };
        var display = await _localService.CreateAsync(localRequest, cancellationToken);
        var serverSaved = await TryCreateOnServerAsync(localRequest, cancellationToken);

        if (!serverSaved)
        {
            await QueueAsync("Create", display.Id, localRequest, cancellationToken);
        }

        return display;
    }

    public async Task<CustomerDisplayDto> UpdateAsync(
        UpdateCustomerDisplayRequest request,
        CancellationToken cancellationToken = default)
    {
        var baseVersion = await _dbContext.CustomerDisplays
            .IgnoreQueryFilters()
            .Where(x => x.Id == request.Id)
            .Select(x => (long?)x.Version)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;
        var localRequest = request with { BaseVersion = baseVersion };
        var display = await _localService.UpdateAsync(localRequest, cancellationToken);
        var serverSaved = await TryUpdateOnServerAsync(localRequest, cancellationToken);

        if (!serverSaved)
        {
            await QueueAsync("Update", display.Id, localRequest, cancellationToken, baseVersion);
        }

        return display;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var baseVersion = await _dbContext.CustomerDisplays
            .IgnoreQueryFilters()
            .Where(x => x.Id == id)
            .Select(x => (long?)x.Version)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;
        var deleted = await _localService.DeleteAsync(id, cancellationToken);
        var serverDeleted = deleted && await TryDeleteOnServerAsync(id, baseVersion, cancellationToken);

        if (deleted && !serverDeleted)
        {
            await QueueAsync("Delete", id, new { Id = id }, cancellationToken, baseVersion);
        }

        return deleted;
    }

    private async Task QueueAsync(string operation, Guid entityId, object payload, CancellationToken cancellationToken, long baseVersion = 0)
    {
        var idempotencyKey = $"customer-display:{entityId}:{operation}";
        var existing = await _dbContext.SyncOutboxItems
            .FirstOrDefaultAsync(
                x => x.IdempotencyKey == idempotencyKey && x.Status != "Failed",
                cancellationToken);

        if (existing?.Status == "Conflict")
        {
            idempotencyKey = $"{idempotencyKey}:{Guid.NewGuid():N}";
            existing = null;
        }

        if (existing is not null)
        {
            existing.PayloadJson = JsonSerializer.Serialize(payload);
            existing.BaseVersion = existing.BaseVersion == 0 ? baseVersion : existing.BaseVersion;
            existing.Status = "Pending";
            existing.Error = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        await _dbContext.SyncOutboxItems.AddAsync(new SyncOutboxItem
        {
            EntityType = $"CustomerDisplay:{operation}",
            EntityId = entityId,
            PayloadJson = JsonSerializer.Serialize(payload),
            IdempotencyKey = idempotencyKey,
            Status = "Pending",
            BaseVersion = baseVersion
        }, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> TryCreateOnServerAsync(CreateCustomerDisplayRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await _apiClient.CreateCustomerDisplayAsync(request, cancellationToken) is not null;
        }
        catch (HttpRequestException) { return false; }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { return false; }
    }

    private async Task<bool> TryUpdateOnServerAsync(UpdateCustomerDisplayRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await _apiClient.UpdateCustomerDisplayAsync(request, cancellationToken) is not null;
        }
        catch (HttpRequestException) { return false; }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { return false; }
    }

    private async Task<bool> TryDeleteOnServerAsync(Guid id, long baseVersion, CancellationToken cancellationToken)
    {
        try
        {
            return await _apiClient.DeleteCustomerDisplayAsync(id, cancellationToken, baseVersion);
        }
        catch (HttpRequestException) { return false; }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { return false; }
    }
}
