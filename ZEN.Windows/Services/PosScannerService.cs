using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZEN.Application.DTOs.Admin;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Infrastructure.Persistence;
using ZEN.Windows.Services.Sync;

namespace ZEN.Windows.Services;

public sealed class PosScannerService
{
    private readonly ZenDbContext _dbContext;
    private readonly IScannerService _localScannerService;
    private readonly SyncApiClient _apiClient;

    public PosScannerService(
        ZenDbContext dbContext,
        IScannerService localScannerService,
        SyncApiClient apiClient)
    {
        _dbContext = dbContext;
        _localScannerService = localScannerService;
        _apiClient = apiClient;
    }

    public Task<IEnumerable<ScannerDto>> GetCachedAsync(CancellationToken cancellationToken = default) =>
        _localScannerService.GetAllAsync(cancellationToken);

    public async Task<IReadOnlyList<ScannerDto>> RefreshAsync(
        Guid? storeId,
        CancellationToken cancellationToken = default)
    {
        var scanners = (await _apiClient.GetScannersAsync(storeId, cancellationToken)).ToList();

        foreach (var dto in scanners)
        {
            var existing = await _dbContext.Scanners
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == dto.Id, cancellationToken);

            if (existing is null)
            {
                existing = new Scanner { Id = dto.Id };
                await _dbContext.Scanners.AddAsync(existing, cancellationToken);
            }

            existing.Name = dto.Name;
            existing.ScannerType = Enum.Parse<ZEN.Domain.Enums.ScannerType>(dto.ScannerType);
            existing.ConnectionType = dto.ConnectionType;
            existing.ConnectionString = dto.ConnectionString;
            existing.CashDeskId = dto.CashDeskId;
            existing.IsActive = dto.IsActive;
            existing.IsDeleted = false;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return scanners;
    }

    public async Task<ScannerDto> CreateAsync(
        CreateScannerRequest request,
        CancellationToken cancellationToken = default)
    {
        var localRequest = request with { Id = request.Id ?? Guid.NewGuid() };
        var serverSaved = await TryCreateOnServerAsync(localRequest, cancellationToken);
        var scanner = await _localScannerService.CreateAsync(localRequest, cancellationToken);

        if (!serverSaved)
        {
            await QueueAsync("Create", scanner.Id, localRequest, cancellationToken);
        }

        return scanner;
    }

    public async Task<ScannerDto> UpdateAsync(
        UpdateScannerRequest request,
        CancellationToken cancellationToken = default)
    {
        var serverSaved = await TryUpdateOnServerAsync(request, cancellationToken);
        var scanner = await _localScannerService.UpdateAsync(request, cancellationToken);

        if (!serverSaved)
        {
            await QueueAsync("Update", scanner.Id, request, cancellationToken);
        }

        return scanner;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var serverDeleted = await TryDeleteOnServerAsync(id, cancellationToken);
        var deleted = await _localScannerService.DeleteAsync(id, cancellationToken);

        if (deleted && !serverDeleted)
        {
            await QueueAsync("Delete", id, new { Id = id }, cancellationToken);
        }

        return deleted;
    }

    private async Task QueueAsync(
        string operation,
        Guid entityId,
        object payload,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"scanner:{entityId}:{operation}";
        var existing = await _dbContext.SyncOutboxItems
            .FirstOrDefaultAsync(
                x => x.IdempotencyKey == idempotencyKey && x.Status != "Failed",
                cancellationToken);

        if (existing is not null)
        {
            existing.PayloadJson = JsonSerializer.Serialize(payload);
            existing.Status = "Pending";
            existing.Error = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        await _dbContext.SyncOutboxItems.AddAsync(new SyncOutboxItem
        {
            EntityType = $"Scanner:{operation}",
            EntityId = entityId,
            PayloadJson = JsonSerializer.Serialize(payload),
            IdempotencyKey = idempotencyKey,
            Status = "Pending"
        }, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> TryCreateOnServerAsync(
        CreateScannerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _apiClient.CreateScannerAsync(request, cancellationToken) is not null;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private async Task<bool> TryUpdateOnServerAsync(
        UpdateScannerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _apiClient.UpdateScannerAsync(request, cancellationToken) is not null;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private async Task<bool> TryDeleteOnServerAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _apiClient.DeleteScannerAsync(id, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }
}
