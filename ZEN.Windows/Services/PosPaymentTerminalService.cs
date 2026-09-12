using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZEN.Application.DTOs.Admin;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Infrastructure.Persistence;
using ZEN.Windows.Services.Sync;

namespace ZEN.Windows.Services;

public sealed class PosPaymentTerminalService
{
    private readonly ZenDbContext _dbContext;
    private readonly IPaymentTerminalService _localService;
    private readonly SyncApiClient _apiClient;

    public PosPaymentTerminalService(
        ZenDbContext dbContext,
        IPaymentTerminalService localService,
        SyncApiClient apiClient)
    {
        _dbContext = dbContext;
        _localService = localService;
        _apiClient = apiClient;
    }

    public Task<IEnumerable<PaymentTerminalDto>> GetCachedAsync(
        CancellationToken cancellationToken = default) =>
        _localService.GetAllAsync(cancellationToken);

    public async Task<IReadOnlyList<PaymentTerminalDto>> RefreshAsync(
        CancellationToken cancellationToken = default)
    {
        var terminals = (await _apiClient.GetPaymentTerminalsAsync(cancellationToken: cancellationToken)).ToList();
        var pendingEntityIds = await _dbContext.SyncOutboxItems
            .Where(x => x.EntityType.StartsWith("PaymentTerminal:")
                && (x.Status == "Pending" || x.Status == "InProgress"))
            .Select(x => x.EntityId)
            .ToHashSetAsync(cancellationToken);
        var pendingDeleteIds = await _dbContext.SyncOutboxItems
            .Where(x => x.EntityType == "PaymentTerminal:Delete"
                && (x.Status == "Pending" || x.Status == "InProgress"))
            .Select(x => x.EntityId)
            .ToHashSetAsync(cancellationToken);
        terminals = terminals
            .Where(x => !pendingDeleteIds.Contains(x.Id))
            .ToList();
        foreach (var dto in terminals)
        {
            if (pendingEntityIds.Contains(dto.Id))
            {
                continue;
            }
            var existing = await _dbContext.PaymentTerminals
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == dto.Id, cancellationToken);

            if (existing is null)
            {
                existing = new PaymentTerminal { Id = dto.Id };
                await _dbContext.PaymentTerminals.AddAsync(existing, cancellationToken);
            }

            existing.Name = dto.Name;
            existing.TerminalType = dto.TerminalType;
            existing.SerialNumber = dto.SerialNumber;
            existing.IpAddress = dto.IpAddress;
            existing.Port = dto.Port;
            existing.CashDeskId = dto.CashDeskId;
            existing.IsActive = dto.IsActive;
            existing.Version = dto.Version;
            existing.IsDeleted = dto.IsDeleted;
        }

        await _dbContext.SaveServerSynchronizationAsync(cancellationToken);
        return terminals;
    }

    public async Task<PaymentTerminalDto> CreateAsync(
        CreatePaymentTerminalRequest request,
        CancellationToken cancellationToken = default)
    {
        var localRequest = request with { Id = request.Id ?? Guid.NewGuid() };
        var terminal = await _localService.CreateAsync(localRequest, cancellationToken);
        var serverSaved = await TryCreateOnServerAsync(localRequest, cancellationToken);

        if (!serverSaved)
        {
            await QueueAsync("Create", terminal.Id, localRequest, cancellationToken);
        }

        return terminal;
    }

    public async Task<PaymentTerminalDto> UpdateAsync(
        UpdatePaymentTerminalRequest request,
        CancellationToken cancellationToken = default)
    {
        var baseVersion = await _dbContext.PaymentTerminals
            .IgnoreQueryFilters()
            .Where(x => x.Id == request.Id)
            .Select(x => (long?)x.Version)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;
        var localRequest = request with { BaseVersion = baseVersion };
        var terminal = await _localService.UpdateAsync(localRequest, cancellationToken);
        var serverSaved = await TryUpdateOnServerAsync(localRequest, cancellationToken);

        if (!serverSaved)
        {
            await QueueAsync("Update", terminal.Id, localRequest, cancellationToken, baseVersion);
        }

        return terminal;
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var baseVersion = await _dbContext.PaymentTerminals
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
        var idempotencyKey = $"payment-terminal:{entityId}:{operation}";
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
            EntityType = $"PaymentTerminal:{operation}",
            EntityId = entityId,
            PayloadJson = JsonSerializer.Serialize(payload),
            IdempotencyKey = idempotencyKey,
            Status = "Pending",
            BaseVersion = baseVersion
        }, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> TryCreateOnServerAsync(
        CreatePaymentTerminalRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _apiClient.CreatePaymentTerminalAsync(request, cancellationToken) is not null;
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
        UpdatePaymentTerminalRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _apiClient.UpdatePaymentTerminalAsync(request, cancellationToken) is not null;
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
            return await _apiClient.DeletePaymentTerminalAsync(id, cancellationToken, baseVersion);
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
