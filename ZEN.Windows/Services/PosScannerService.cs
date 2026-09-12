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
        var pendingEntityIds = await _dbContext.SyncOutboxItems
            .Where(x => x.EntityType.StartsWith("Scanner:")
                && (x.Status == "Pending" || x.Status == "InProgress"))
            .Select(x => x.EntityId)
            .ToHashSetAsync(cancellationToken);
        var pendingDeleteIds = await _dbContext.SyncOutboxItems
            .Where(x => x.EntityType == "Scanner:Delete"
                && (x.Status == "Pending" || x.Status == "InProgress"))
            .Select(x => x.EntityId)
            .ToHashSetAsync(cancellationToken);
        scanners = scanners
            .Where(x => !pendingDeleteIds.Contains(x.Id))
            .ToList();
        var serverIds = scanners.Select(s => s.Id).ToHashSet();
        var staleScanners = await _dbContext.Scanners
            .IgnoreQueryFilters()
            .Where(s => !s.IsDeleted && !serverIds.Contains(s.Id)
                && !pendingEntityIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        foreach (var staleScanner in staleScanners)
        {
            staleScanner.IsDeleted = true;
        }

        foreach (var dto in scanners)
        {
            if (pendingDeleteIds.Contains(dto.Id))
            {
                continue;
            }
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
        var serverSaved = false;
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
        var serverSaved = false;
        var scanner = await _localScannerService.UpdateAsync(request, cancellationToken);

        if (!serverSaved)
        {
            await QueueAsync("Update", scanner.Id, request, cancellationToken);
        }

        return scanner;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var serverDeleted = false;
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

