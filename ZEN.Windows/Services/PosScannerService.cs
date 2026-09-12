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
        foreach (var dto in scanners)
        {
            if (pendingEntityIds.Contains(dto.Id))
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
            existing.Version = dto.Version;
            existing.IsDeleted = dto.IsDeleted;
        }

        await _dbContext.SaveServerSynchronizationAsync(cancellationToken);
        return scanners;
    }

    public async Task<ScannerDto> CreateAsync(
        CreateScannerRequest request,
        CancellationToken cancellationToken = default)
    {
        var localRequest = request with { Id = request.Id ?? Guid.NewGuid() };
        var scanner = await _localScannerService.CreateAsync(localRequest, cancellationToken);
        var serverSaved = await TryCreateOnServerAsync(localRequest, cancellationToken);

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
        var baseVersion = await _dbContext.Scanners
            .IgnoreQueryFilters()
            .Where(x => x.Id == request.Id)
            .Select(x => (long?)x.Version)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;
        var localRequest = request with { BaseVersion = baseVersion };
        var scanner = await _localScannerService.UpdateAsync(localRequest, cancellationToken);
        var serverSaved = await TryUpdateOnServerAsync(localRequest, cancellationToken);

        if (!serverSaved)
        {
            await QueueAsync("Update", scanner.Id, localRequest, cancellationToken, baseVersion);
        }

        return scanner;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var baseVersion = await _dbContext.Scanners
            .IgnoreQueryFilters()
            .Where(x => x.Id == id)
            .Select(x => (long?)x.Version)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;
        var deleted = await _localScannerService.DeleteAsync(id, cancellationToken);
        var serverDeleted = deleted && await TryDeleteOnServerAsync(id, baseVersion, cancellationToken);

        if (deleted && !serverDeleted)
        {
            await QueueAsync("Delete", id, new { Id = id }, cancellationToken, baseVersion);
        }

        return deleted;
    }

    private async Task QueueAsync(
        string operation,
        Guid entityId,
        object payload,
        CancellationToken cancellationToken,
        long baseVersion = 0)
    {
        var idempotencyKey = $"scanner:{entityId}:{operation}";
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
            EntityType = $"Scanner:{operation}",
            EntityId = entityId,
            PayloadJson = JsonSerializer.Serialize(payload),
            IdempotencyKey = idempotencyKey,
            Status = "Pending",
            BaseVersion = baseVersion
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
        long baseVersion,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _apiClient.DeleteScannerAsync(id, cancellationToken, baseVersion);
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
