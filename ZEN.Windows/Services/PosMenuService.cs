using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZEN.Application.DTOs.CashDeskMenu;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Infrastructure.Persistence;
using ZEN.Windows.Services.Sync;

namespace ZEN.Windows.Services;

public sealed class PosMenuService
{
    private readonly ZenDbContext _dbContext;
    private readonly IMenuService _localService;
    private readonly SyncApiClient _apiClient;

    public PosMenuService(
        ZenDbContext dbContext,
        IMenuService localService,
        SyncApiClient apiClient)
    {
        _dbContext = dbContext;
        _localService = localService;
        _apiClient = apiClient;
    }

    public Task<IEnumerable<MenuSummaryDto>> GetCachedAsync(
        CancellationToken cancellationToken = default) =>
        _localService.GetAllSummariesAsync(cancellationToken);

    public async Task<IReadOnlyList<MenuSummaryDto>> RefreshAsync(
        CancellationToken cancellationToken = default)
    {
        var menus = (await _apiClient.GetMenusAsync(cancellationToken)).ToList();
        var pendingEntityIds = await _dbContext.SyncOutboxItems
            .Where(x => x.EntityType.StartsWith("Menu:")
                && (x.Status == "Pending" || x.Status == "InProgress"))
            .Select(x => x.EntityId)
            .ToHashSetAsync(cancellationToken);
        var pendingDeleteIds = await _dbContext.SyncOutboxItems
            .Where(x => x.EntityType == "Menu:Delete"
                && (x.Status == "Pending" || x.Status == "InProgress"))
            .Select(x => x.EntityId)
            .ToHashSetAsync(cancellationToken);
        menus = menus
            .Where(x => !pendingDeleteIds.Contains(x.Id))
            .ToList();
        foreach (var dto in menus)
        {
            if (pendingEntityIds.Contains(dto.Id))
            {
                continue;
            }
            var existing = await _dbContext.Menus
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.Id == dto.Id, cancellationToken);

            if (existing is null)
            {
                existing = new Menu { Id = dto.Id };
                await _dbContext.Menus.AddAsync(existing, cancellationToken);
            }

            existing.Name = dto.Name;
            existing.StoreId = dto.StoreId;
            existing.Version = dto.Version;
            existing.IsDeleted = dto.IsDeleted;
        }

        await _dbContext.SaveServerSynchronizationAsync(cancellationToken);
        return menus;
    }

    public async Task<MenuDto> CreateAsync(
        CreateMenuRequest request,
        CancellationToken cancellationToken = default)
    {
        var localRequest = request with { Id = request.Id ?? Guid.NewGuid() };
        var menu = await _localService.CreateAsync(localRequest, cancellationToken);
        var serverSaved = await TryCreateOnServerAsync(localRequest, cancellationToken);

        if (!serverSaved)
            await QueueAsync("Create", menu.Id, localRequest, cancellationToken);

        return menu;
    }

    public async Task<MenuDto> UpdateAsync(
        UpdateMenuRequest request,
        CancellationToken cancellationToken = default)
    {
        var baseVersion = await _dbContext.Menus
            .IgnoreQueryFilters()
            .Where(x => x.Id == request.Id)
            .Select(x => (long?)x.Version)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;
        var localRequest = request with { BaseVersion = baseVersion };
        var menu = await _localService.UpdateAsync(localRequest, cancellationToken);
        var serverSaved = await TryUpdateOnServerAsync(localRequest, cancellationToken);

        if (!serverSaved)
            await QueueAsync("Update", menu.Id, localRequest, cancellationToken, baseVersion);

        return menu;
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var baseVersion = await _dbContext.Menus
            .IgnoreQueryFilters()
            .Where(x => x.Id == id)
            .Select(x => (long?)x.Version)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;
        var deleted = await _localService.DeleteAsync(id, cancellationToken);
        var serverDeleted = deleted && await TryDeleteOnServerAsync(id, baseVersion, cancellationToken);

        if (deleted && !serverDeleted)
            await QueueAsync("Delete", id, new { Id = id }, cancellationToken, baseVersion);

        return deleted;
    }

    private async Task QueueAsync(
        string operation,
        Guid entityId,
        object payload,
        CancellationToken cancellationToken,
        long baseVersion = 0)
    {
        var idempotencyKey = $"menu:{entityId}:{operation}";
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
        }
        else
        {
            await _dbContext.SyncOutboxItems.AddAsync(new SyncOutboxItem
            {
                EntityType = $"Menu:{operation}",
                EntityId = entityId,
                PayloadJson = JsonSerializer.Serialize(payload),
                IdempotencyKey = idempotencyKey,
                Status = "Pending",
                BaseVersion = baseVersion
            }, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> TryCreateOnServerAsync(CreateMenuRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await _apiClient.CreateMenuAsync(request, cancellationToken) is not null;
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

    private async Task<bool> TryUpdateOnServerAsync(UpdateMenuRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await _apiClient.UpdateMenuAsync(request, cancellationToken) is not null;
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

    private async Task<bool> TryDeleteOnServerAsync(Guid id, long baseVersion, CancellationToken cancellationToken)
    {
        try
        {
            return await _apiClient.DeleteMenuAsync(id, cancellationToken, baseVersion);
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
