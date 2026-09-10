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

        foreach (var dto in displays)
        {
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
            existing.IsDeleted = false;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return displays;
    }

    public async Task<CustomerDisplayDto> CreateAsync(
        CreateCustomerDisplayRequest request,
        CancellationToken cancellationToken = default)
    {
        var localRequest = request with { Id = request.Id ?? Guid.NewGuid() };
        var serverSaved = await TryCreateOnServerAsync(localRequest, cancellationToken);
        var display = await _localService.CreateAsync(localRequest, cancellationToken);

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
        var serverSaved = await TryUpdateOnServerAsync(request, cancellationToken);
        var display = await _localService.UpdateAsync(request, cancellationToken);

        if (!serverSaved)
        {
            await QueueAsync("Update", display.Id, request, cancellationToken);
        }

        return display;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var serverDeleted = await TryDeleteOnServerAsync(id, cancellationToken);
        var deleted = await _localService.DeleteAsync(id, cancellationToken);

        if (deleted && !serverDeleted)
        {
            await QueueAsync("Delete", id, new { Id = id }, cancellationToken);
        }

        return deleted;
    }

    private async Task QueueAsync(string operation, Guid entityId, object payload, CancellationToken cancellationToken)
    {
        var idempotencyKey = $"customer-display:{entityId}:{operation}";
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
            EntityType = $"CustomerDisplay:{operation}",
            EntityId = entityId,
            PayloadJson = JsonSerializer.Serialize(payload),
            IdempotencyKey = idempotencyKey,
            Status = "Pending"
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

    private async Task<bool> TryDeleteOnServerAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return await _apiClient.DeleteCustomerDisplayAsync(id, cancellationToken);
        }
        catch (HttpRequestException) { return false; }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { return false; }
    }
}
