using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZEN.Application.DTOs.Admin;
using ZEN.Domain.Entities;
using ZEN.Infrastructure.Persistence;
using ZEN.Windows.Services.Sync;

namespace ZEN.Windows.Services;

public sealed class PosStoreService
{
    private readonly ZenDbContext _dbContext;
    private readonly SyncApiClient _apiClient;

    public PosStoreService(ZenDbContext dbContext, SyncApiClient apiClient)
    {
        _dbContext = dbContext;
        _apiClient = apiClient;
    }

    public async Task<IReadOnlyList<StoreDto>> GetCachedAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Stores
            .AsNoTracking()
            .Select(s => new StoreDto(
                s.Id,
                s.Name,
                s.Code,
                s.Address,
                s.City,
                s.Phone,
                s.Email,
                s.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StoreDto>> RefreshAsync(
        CancellationToken cancellationToken = default)
    {
        var stores = (await _apiClient.GetStoresAsync(cancellationToken)).ToList();
        foreach (var dto in stores)
        {
            var existing = await _dbContext.Stores
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == dto.Id, cancellationToken);

            if (existing is null)
            {
                existing = new Store { Id = dto.Id };
                await _dbContext.Stores.AddAsync(existing, cancellationToken);
            }

            existing.Name = dto.Name;
            existing.Code = dto.Code;
            existing.Address = dto.Address;
            existing.City = dto.City;
            existing.Phone = dto.Phone;
            existing.Email = dto.Email;
            existing.IsActive = dto.IsActive;
            existing.IsDeleted = false;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return stores;
    }

    public async Task<StoreDto> CreateAsync(
        CreateStoreRequest request,
        CancellationToken cancellationToken = default)
    {
        var localRequest = request with { Id = request.Id ?? Guid.NewGuid() };
        var serverSaved = await TryCreateOnServerAsync(localRequest, cancellationToken);
        var store = new Store
        {
            Id = localRequest.Id.Value,
            Name = localRequest.Name,
            Code = localRequest.Code,
            Address = localRequest.Address,
            City = localRequest.City,
            Phone = localRequest.Phone,
            Email = localRequest.Email,
            IsActive = true
        };

        await _dbContext.Stores.AddAsync(store, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!serverSaved)
        {
            await QueueAsync("Create", store.Id, localRequest, cancellationToken);
        }

        return ToDto(store);
    }

    public async Task<StoreDto> UpdateAsync(
        UpdateStoreRequest request,
        CancellationToken cancellationToken = default)
    {
        var serverSaved = await TryUpdateOnServerAsync(request, cancellationToken);
        var store = await _dbContext.Stores.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Store with ID {request.Id} not found.");

        store.Name = request.Name;
        store.Code = request.Code;
        store.Address = request.Address;
        store.City = request.City;
        store.Phone = request.Phone;
        store.Email = request.Email;
        store.IsActive = request.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!serverSaved)
        {
            await QueueAsync("Update", store.Id, request, cancellationToken);
        }

        return ToDto(store);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var serverDeleted = await TryDeleteOnServerAsync(id, cancellationToken);
        var store = await _dbContext.Stores.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (store is null)
            return false;

        _dbContext.Stores.Remove(store);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!serverDeleted)
        {
            await QueueAsync("Delete", id, new { Id = id }, cancellationToken);
        }

        return true;
    }

    private async Task QueueAsync(
        string operation,
        Guid entityId,
        object payload,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"store:{entityId}:{operation}";
        var existing = await _dbContext.SyncOutboxItems
            .FirstOrDefaultAsync(
                x => x.IdempotencyKey == idempotencyKey && x.Status != "Failed",
                cancellationToken);

        if (existing is not null)
        {
            existing.PayloadJson = JsonSerializer.Serialize(payload);
            existing.Status = "Pending";
            existing.Error = null;
        }
        else
        {
            await _dbContext.SyncOutboxItems.AddAsync(new SyncOutboxItem
            {
                EntityType = $"Store:{operation}",
                EntityId = entityId,
                PayloadJson = JsonSerializer.Serialize(payload),
                IdempotencyKey = idempotencyKey,
                Status = "Pending"
            }, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> TryCreateOnServerAsync(
        CreateStoreRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _apiClient.CreateStoreAsync(request, cancellationToken) is not null;
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
        UpdateStoreRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _apiClient.UpdateStoreAsync(request, cancellationToken) is not null;
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
            return await _apiClient.DeleteStoreAsync(id, cancellationToken);
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

    private static StoreDto ToDto(Store store) => new(
        store.Id,
        store.Name,
        store.Code,
        store.Address,
        store.City,
        store.Phone,
        store.Email,
        store.IsActive);
}
