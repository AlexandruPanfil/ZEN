using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZEN.Application.DTOs.Admin;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Infrastructure.Persistence;
using ZEN.Windows.Services.Sync;

namespace ZEN.Windows.Services;

public sealed class PosPrinterService
{
    private readonly ZenDbContext _dbContext;
    private readonly IPrinterService _localPrinterService;
    private readonly SyncApiClient _apiClient;

    public PosPrinterService(
        ZenDbContext dbContext,
        IPrinterService localPrinterService,
        SyncApiClient apiClient)
    {
        _dbContext = dbContext;
        _localPrinterService = localPrinterService;
        _apiClient = apiClient;
    }

    public Task<IEnumerable<PrinterDto>> GetCachedAsync(CancellationToken cancellationToken = default) =>
        _localPrinterService.GetAllAsync(cancellationToken);

    public async Task<IReadOnlyList<PrinterDto>> RefreshAsync(
        Guid? storeId,
        CancellationToken cancellationToken = default)
    {
        var printers = (await _apiClient.GetPrintersAsync(storeId, cancellationToken)).ToList();
        var pendingEntityIds = await _dbContext.SyncOutboxItems
            .Where(x => x.EntityType.StartsWith("Printer:")
                && (x.Status == "Pending" || x.Status == "InProgress"))
            .Select(x => x.EntityId)
            .ToHashSetAsync(cancellationToken);
        var pendingDeleteIds = await _dbContext.SyncOutboxItems
            .Where(x => x.EntityType == "Printer:Delete"
                && (x.Status == "Pending" || x.Status == "InProgress"))
            .Select(x => x.EntityId)
            .ToHashSetAsync(cancellationToken);

        printers = printers
            .Where(x => !pendingDeleteIds.Contains(x.Id))
            .ToList();
        var serverEntityIds = printers.Select(x => x.Id).ToHashSet();
        foreach (var dto in printers)
        {
            if (pendingEntityIds.Contains(dto.Id))
            {
                continue;
            }

            var existing = await _dbContext.Printers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == dto.Id, cancellationToken);

            if (existing is null)
            {
                existing = new Printer { Id = dto.Id };
                await _dbContext.Printers.AddAsync(existing, cancellationToken);
            }

            existing.Name = dto.Name;
            existing.Type = Enum.Parse<ZEN.Domain.Enums.PrinterType>(dto.PrinterType);
            existing.FiscalDeviceType = string.IsNullOrWhiteSpace(dto.FiscalDeviceType)
                ? null
                : Enum.Parse<ZEN.Domain.Enums.FiscalDeviceType>(dto.FiscalDeviceType);
            existing.IsDefault = dto.IsDefault;
            existing.IsActive = dto.IsActive;
            existing.ApiKey = dto.ApiKey;
            existing.ApiSecret = dto.ApiSecret;
            existing.DeviceId = dto.DeviceId;
            existing.PointOfSaleId = dto.PointOfSaleId;
            existing.FiscalCloudBaseUrl = dto.FiscalCloudBaseUrl;
            existing.Version = dto.Version;
            existing.IsDeleted = dto.IsDeleted;
        }

        var missingLocalPrinters = await _dbContext.Printers
            .IgnoreQueryFilters()
            .Where(x => !pendingEntityIds.Contains(x.Id) && !serverEntityIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
        foreach (var printer in missingLocalPrinters)
        {
            printer.IsDeleted = true;
        }

        await _dbContext.SaveServerSynchronizationAsync(cancellationToken);
        return printers.Where(x => !x.IsDeleted).ToList();
    }

    public async Task<PrinterDto> CreateAsync(
        CreatePrinterRequest request,
        CancellationToken cancellationToken = default)
    {
        var localRequest = request with { Id = request.Id ?? Guid.NewGuid() };
        var printer = await _localPrinterService.CreateAsync(localRequest, cancellationToken);
        var serverSaved = await TryCreateOnServerAsync(localRequest, cancellationToken);
        if (!serverSaved)
        {
            await QueueAsync("Create", printer.Id, localRequest, cancellationToken);
        }

        return printer;
    }

    public async Task<PrinterDto> UpdateAsync(
        UpdatePrinterRequest request,
        CancellationToken cancellationToken = default)
    {
        var baseVersion = await _dbContext.Printers
            .IgnoreQueryFilters()
            .Where(x => x.Id == request.Id)
            .Select(x => (long?)x.Version)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;
        var localRequest = request with { BaseVersion = baseVersion };
        var printer = await _localPrinterService.UpdateAsync(localRequest, cancellationToken);
        var serverSaved = await TryUpdateOnServerAsync(localRequest, cancellationToken);
        if (!serverSaved)
        {
            await QueueAsync("Update", printer.Id, localRequest, cancellationToken, baseVersion);
        }

        return printer;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var baseVersion = await _dbContext.Printers
            .IgnoreQueryFilters()
            .Where(x => x.Id == id)
            .Select(x => (long?)x.Version)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;
        var deleted = await _localPrinterService.DeleteAsync(id, cancellationToken);
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
        var idempotencyKey = $"printer:{entityId}:{operation}";
        var existing = await _dbContext.SyncOutboxItems
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey && x.Status != "Failed", cancellationToken);

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
            EntityType = $"Printer:{operation}",
            EntityId = entityId,
            PayloadJson = JsonSerializer.Serialize(payload),
            IdempotencyKey = idempotencyKey,
            Status = "Pending",
            BaseVersion = baseVersion
        }, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> TryCreateOnServerAsync(
        CreatePrinterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _apiClient.CreatePrinterAsync(request, cancellationToken) is not null;
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
        UpdatePrinterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _apiClient.UpdatePrinterAsync(request, cancellationToken) is not null;
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
            return await _apiClient.DeletePrinterAsync(id, cancellationToken, baseVersion);
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

