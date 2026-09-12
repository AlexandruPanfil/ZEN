using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZEN.Application.DTOs.Admin;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Infrastructure.Persistence;
using ZEN.Windows.Services.Sync;

namespace ZEN.Windows.Services;

public sealed class PosCashDeskService
{
    private readonly ZenDbContext _dbContext;
    private readonly ICashDeskService _localService;
    private readonly SyncApiClient _apiClient;

    public PosCashDeskService(
        ZenDbContext dbContext,
        ICashDeskService localService,
        SyncApiClient apiClient)
    {
        _dbContext = dbContext;
        _localService = localService;
        _apiClient = apiClient;
    }

    public Task<IEnumerable<CashDeskDto>> GetCachedAsync(
        Guid? storeId = null,
        CancellationToken cancellationToken = default) =>
        _localService.GetAllAsync(storeId, cancellationToken);

    public async Task<IReadOnlyList<CashDeskDto>> RefreshAsync(
        Guid? storeId,
        CancellationToken cancellationToken = default)
    {
        var cashDesks = (await _apiClient.GetCashDesksAsync(storeId, cancellationToken)).ToList();
        var pendingEntityIds = await _dbContext.SyncOutboxItems
            .Where(x => x.EntityType.StartsWith("CashDesk:")
                && (x.Status == "Pending" || x.Status == "InProgress"))
            .Select(x => x.EntityId)
            .ToHashSetAsync(cancellationToken);
        var pendingDeleteIds = await _dbContext.SyncOutboxItems
            .Where(x => x.EntityType == "CashDesk:Delete"
                && (x.Status == "Pending" || x.Status == "InProgress"))
            .Select(x => x.EntityId)
            .ToHashSetAsync(cancellationToken);
        cashDesks = cashDesks
            .Where(x => !pendingDeleteIds.Contains(x.Id))
            .ToList();
        foreach (var dto in cashDesks)
        {
            if (pendingEntityIds.Contains(dto.Id))
            {
                continue;
            }
            var existing = await _dbContext.CashDesks
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(cd => cd.Id == dto.Id, cancellationToken);

            if (existing is null)
            {
                existing = new CashDesk { Id = dto.Id };
                await _dbContext.CashDesks.AddAsync(existing, cancellationToken);
            }

            existing.Name = dto.Name;
            existing.StoreId = dto.StoreId;
            existing.IsActive = dto.IsActive;
            existing.FiscalPrinterId = dto.FiscalPrinterId;
            existing.CustomerDisplayId = dto.CustomerDisplayId;
            existing.LastActivityUtc = dto.LastActivityUtc;
            existing.IsOnline = dto.IsOnline;
            existing.MenuId = dto.MenuId;
            existing.Version = dto.Version;
            existing.IsDeleted = dto.IsDeleted;
        }

        await _dbContext.SaveServerSynchronizationAsync(cancellationToken);
        return cashDesks;
    }

    public Task<CashDeskDetailDto?> GetDetailAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        _localService.GetDetailByIdAsync(id, cancellationToken);

    public async Task<CashDeskDto> CreateAsync(
        CreateCashDeskRequest request,
        CancellationToken cancellationToken = default)
    {
        var localRequest = request with { Id = request.Id ?? Guid.NewGuid() };
        var cashDesk = await _localService.CreateAsync(localRequest, cancellationToken);
        var serverSaved = await TryCreateOnServerAsync(localRequest, cancellationToken);

        if (!serverSaved)
        {
            await QueueAsync("Create", cashDesk.Id, localRequest, cancellationToken);
        }

        return cashDesk;
    }

    public async Task<CashDeskDto> UpdateAsync(
        UpdateCashDeskRequest request,
        CancellationToken cancellationToken = default)
    {
        var baseVersion = await _dbContext.CashDesks
            .IgnoreQueryFilters()
            .Where(x => x.Id == request.Id)
            .Select(x => (long?)x.Version)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;
        var localRequest = request with { BaseVersion = baseVersion };
        var cashDesk = await _localService.UpdateAsync(localRequest, cancellationToken);
        var serverSaved = await TryUpdateOnServerAsync(localRequest, cancellationToken);

        if (!serverSaved)
        {
            await QueueAsync("Update", cashDesk.Id, localRequest, cancellationToken, baseVersion);
        }

        return cashDesk;
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var baseVersion = await _dbContext.CashDesks
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

    private async Task QueueAsync(
        string operation,
        Guid entityId,
        object payload,
        CancellationToken cancellationToken,
        long baseVersion = 0)
    {
        var idempotencyKey = $"cash-desk:{entityId}:{operation}";
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
            EntityType = $"CashDesk:{operation}",
            EntityId = entityId,
            PayloadJson = JsonSerializer.Serialize(payload),
            IdempotencyKey = idempotencyKey,
            Status = "Pending",
            BaseVersion = baseVersion
        }, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> TryCreateOnServerAsync(
        CreateCashDeskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _apiClient.CreateCashDeskAsync(request, cancellationToken) is not null;
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
        UpdateCashDeskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _apiClient.UpdateCashDeskAsync(request, cancellationToken) is not null;
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
            return await _apiClient.DeleteCashDeskAsync(id, cancellationToken, baseVersion);
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
