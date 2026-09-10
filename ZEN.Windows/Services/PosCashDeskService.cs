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

        foreach (var dto in cashDesks)
        {
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
            existing.IsDeleted = false;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
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
        var serverSaved = await TryCreateOnServerAsync(localRequest, cancellationToken);
        var cashDesk = await _localService.CreateAsync(localRequest, cancellationToken);

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
        var serverSaved = await TryUpdateOnServerAsync(request, cancellationToken);
        var cashDesk = await _localService.UpdateAsync(request, cancellationToken);

        if (!serverSaved)
        {
            await QueueAsync("Update", cashDesk.Id, request, cancellationToken);
        }

        return cashDesk;
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var serverDeleted = await TryDeleteOnServerAsync(id, cancellationToken);
        var deleted = await _localService.DeleteAsync(id, cancellationToken);

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
        var idempotencyKey = $"cash-desk:{entityId}:{operation}";
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
            EntityType = $"CashDesk:{operation}",
            EntityId = entityId,
            PayloadJson = JsonSerializer.Serialize(payload),
            IdempotencyKey = idempotencyKey,
            Status = "Pending"
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
        CancellationToken cancellationToken)
    {
        try
        {
            return await _apiClient.DeleteCashDeskAsync(id, cancellationToken);
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
