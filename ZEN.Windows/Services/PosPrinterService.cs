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

        foreach (var dto in printers)
        {
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
            existing.IsDeleted = false;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return printers;
    }

    public async Task<PrinterDto> CreateAsync(
        CreatePrinterRequest request,
        CancellationToken cancellationToken = default)
    {
        var localRequest = request with { Id = request.Id ?? Guid.NewGuid() };
        var serverSaved = await TryCreateOnServerAsync(localRequest, cancellationToken);
        var printer = await _localPrinterService.CreateAsync(localRequest, cancellationToken);
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
        var serverSaved = await TryUpdateOnServerAsync(request, cancellationToken);
        var printer = await _localPrinterService.UpdateAsync(request, cancellationToken);
        if (!serverSaved)
        {
            await QueueAsync("Update", printer.Id, request, cancellationToken);
        }

        return printer;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var serverDeleted = await TryDeleteOnServerAsync(id, cancellationToken);
        var deleted = await _localPrinterService.DeleteAsync(id, cancellationToken);
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
        var idempotencyKey = $"printer:{entityId}:{operation}";
        var existing = await _dbContext.SyncOutboxItems
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey && x.Status != "Failed", cancellationToken);

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
            EntityType = $"Printer:{operation}",
            EntityId = entityId,
            PayloadJson = JsonSerializer.Serialize(payload),
            IdempotencyKey = idempotencyKey,
            Status = "Pending"
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
        CancellationToken cancellationToken)
    {
        try
        {
            return await _apiClient.DeletePrinterAsync(id, cancellationToken);
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
