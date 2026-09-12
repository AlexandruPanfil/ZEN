using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZEN.Application.DTOs.Admin;
using ZEN.Application.DTOs.CashDeskMenu;
using ZEN.Application.DTOs.Sync;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Infrastructure.Persistence;
using ZEN.Windows.Services;

namespace ZEN.Windows.Services.Sync;

public class PosSyncService : IPosSyncService
{
    private readonly ZenDbContext _dbContext;
    private readonly SyncApiClient _syncApiClient;
    private readonly ILogger<PosSyncService> _logger;
    private readonly PosPrinterService _printerService;
    private readonly PosScannerService _scannerService;
    private readonly PosCustomerDisplayService _customerDisplayService;
    private readonly PosCashDeskService _cashDeskService;
    private readonly PosPaymentTerminalService _paymentTerminalService;
    private readonly PosStoreService _storeService;
    private readonly PosMenuService _menuService;
    private readonly PosAuthState _authState;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public PosSyncService(
        ZenDbContext dbContext,
        SyncApiClient syncApiClient,
        ILogger<PosSyncService> logger,
        PosPrinterService printerService,
        PosScannerService scannerService,
        PosCustomerDisplayService customerDisplayService,
        PosCashDeskService cashDeskService,
        PosPaymentTerminalService paymentTerminalService,
        PosStoreService storeService,
        PosMenuService menuService,
        PosAuthState authState)
    {
        _dbContext = dbContext;
        _syncApiClient = syncApiClient;
        _logger = logger;
        _printerService = printerService;
        _scannerService = scannerService;
        _customerDisplayService = customerDisplayService;
        _cashDeskService = cashDeskService;
        _paymentTerminalService = paymentTerminalService;
        _storeService = storeService;
        _menuService = menuService;
        _authState = authState;
    }

    public async Task RunSyncAsync(CancellationToken cancellationToken = default)
    {
        if (!await _syncLock.WaitAsync(0, cancellationToken))
        {
            _logger.LogInformation("Sync skipped because another sync run is already in progress.");
            return;
        }

        try
        {
            var syncState = await GetOrCreateSyncStateAsync(cancellationToken);

            if (!_syncApiClient.HasBearerToken)
            {
                syncState.LastError = "Synchronization is waiting for a server login token.";
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("POS sync skipped because no server bearer token is available. Local changes remain queued.");
                return;
            }

            await RecoverStaleOutboxItemsAsync(cancellationToken);
            var pendingCount = await _dbContext.SyncOutboxItems
                .CountAsync(x => x.Status == "Pending", cancellationToken);
            _logger.LogInformation("POS sync started with {PendingCount} pending outbox item(s) for {User}.",
                pendingCount,
                _authState.UserName ?? "unknown user");

            await PushPendingPaymentsAsync(syncState, cancellationToken);
            await PushPendingPrintersAsync(cancellationToken);
            await PushPendingScannersAsync(cancellationToken);
            await PushPendingCustomerDisplaysAsync(cancellationToken);
            await PushPendingCashDesksAsync(cancellationToken);
            await PushPendingPaymentTerminalsAsync(cancellationToken);
            await PushPendingStoresAsync(cancellationToken);
            await PushPendingMenusAsync(cancellationToken);
            await PullEquipmentAsync(cancellationToken);
            await PullProductsAsync(syncState, cancellationToken);

            syncState.LastSuccessfulSyncUtc = DateTime.UtcNow;
            syncState.LastError = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        catch (Exception ex)
        {
            var syncState = await GetOrCreateSyncStateAsync(cancellationToken);
            syncState.LastError = ex.Message;
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, "POS sync run failed.");
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private async Task RecoverStaleOutboxItemsAsync(CancellationToken cancellationToken)
    {
        var cutoffUtc = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10));
        var staleItems = await _dbContext.SyncOutboxItems
            .Where(x => x.Status == "InProgress"
                && (!x.LastAttemptUtc.HasValue || x.LastAttemptUtc.Value < cutoffUtc))
            .ToListAsync(cancellationToken);

        if (staleItems.Count == 0)
        {
            return;
        }

        foreach (var item in staleItems)
        {
            item.Status = "Pending";
            item.Error = "Recovered after an interrupted synchronization attempt.";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogWarning("Recovered {Count} stale outbox item(s) left InProgress.", staleItems.Count);
    }

    private async Task PushPendingPrintersAsync(CancellationToken cancellationToken)
    {
        var pending = await _dbContext.SyncOutboxItems
            .Where(x => x.EntityType.StartsWith("Printer:") && x.Status == "Pending")
            .OrderBy(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var outbox in pending)
        {
            outbox.Status = "InProgress";
            outbox.LastAttemptUtc = DateTime.UtcNow;
            outbox.RetryCount++;
            await _dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                var succeeded = outbox.EntityType switch
                {
                    "Printer:Create" => await PushPrinterCreateAsync(outbox, cancellationToken),
                    "Printer:Update" => await PushPrinterUpdateAsync(outbox, cancellationToken),
                    "Printer:Delete" => await _syncApiClient.DeletePrinterAsync(outbox.EntityId, cancellationToken, outbox.BaseVersion ?? 0),
                    _ => throw new InvalidOperationException($"Unknown printer sync operation '{outbox.EntityType}'.")
                };

                outbox.Status = succeeded ? "Synced" : "Pending";
                outbox.SyncedAtUtc = succeeded ? DateTime.UtcNow : null;
                outbox.Error = succeeded ? null : "Server rejected printer synchronization.";
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                outbox.Status = "Failed";
                outbox.SyncedAtUtc = null;
                outbox.Error = ex.Message;
                _logger.LogWarning("Printer synchronization was not applied because printer {PrinterId} does not exist on the server.", outbox.EntityId);
            }
            catch (HttpRequestException ex)
            {
                outbox.Status = ex.StatusCode == System.Net.HttpStatusCode.Conflict ? "Conflict" : "Pending";
                outbox.SyncedAtUtc = null;
                outbox.Error = ex.Message;
                SetConflictDetails(outbox, ex);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                outbox.Status = "Pending";
                outbox.Error = "Printer synchronization timed out.";
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> PushPrinterCreateAsync(
        SyncOutboxItem outbox,
        CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<CreatePrinterRequest>(outbox.PayloadJson)
            ?? throw new InvalidOperationException("Invalid printer create payload.");
        return await _syncApiClient.CreatePrinterAsync(request, cancellationToken) is not null;
    }

    private async Task<bool> PushPrinterUpdateAsync(
        SyncOutboxItem outbox,
        CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<UpdatePrinterRequest>(outbox.PayloadJson)
            ?? throw new InvalidOperationException("Invalid printer update payload.");
        request = request with { BaseVersion = (outbox.BaseVersion ?? 0) != 0 ? outbox.BaseVersion.GetValueOrDefault() : request.BaseVersion };
        return await _syncApiClient.UpdatePrinterAsync(request, cancellationToken) is not null;
    }

    private async Task PushPendingScannersAsync(CancellationToken cancellationToken)
    {
        var pending = await _dbContext.SyncOutboxItems
            .Where(x => x.EntityType.StartsWith("Scanner:") && x.Status == "Pending")
            .OrderBy(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var outbox in pending)
        {
            outbox.Status = "InProgress";
            outbox.LastAttemptUtc = DateTime.UtcNow;
            outbox.RetryCount++;
            await _dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                var succeeded = outbox.EntityType switch
                {
                    "Scanner:Create" => await PushScannerCreateAsync(outbox, cancellationToken),
                    "Scanner:Update" => await PushScannerUpdateAsync(outbox, cancellationToken),
                    "Scanner:Delete" => await _syncApiClient.DeleteScannerAsync(outbox.EntityId, cancellationToken, outbox.BaseVersion ?? 0),
                    _ => throw new InvalidOperationException($"Unknown scanner sync operation '{outbox.EntityType}'.")
                };

                outbox.Status = succeeded ? "Synced" : "Pending";
                outbox.SyncedAtUtc = succeeded ? DateTime.UtcNow : null;
                outbox.Error = succeeded ? null : "Server rejected scanner synchronization.";
            }
            catch (HttpRequestException ex)
            {
                outbox.Status = ex.StatusCode == System.Net.HttpStatusCode.Conflict ? "Conflict" : "Pending";
                outbox.SyncedAtUtc = null;
                outbox.Error = ex.Message;
                SetConflictDetails(outbox, ex);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                outbox.Status = "Pending";
                outbox.Error = "Scanner synchronization timed out.";
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> PushScannerCreateAsync(
        SyncOutboxItem outbox,
        CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<CreateScannerRequest>(outbox.PayloadJson)
            ?? throw new InvalidOperationException("Invalid scanner create payload.");
        return await _syncApiClient.CreateScannerAsync(request, cancellationToken) is not null;
    }

    private async Task<bool> PushScannerUpdateAsync(
        SyncOutboxItem outbox,
        CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<UpdateScannerRequest>(outbox.PayloadJson)
            ?? throw new InvalidOperationException("Invalid scanner update payload.");
        request = request with { BaseVersion = (outbox.BaseVersion ?? 0) != 0 ? outbox.BaseVersion.GetValueOrDefault() : request.BaseVersion };
        return await _syncApiClient.UpdateScannerAsync(request, cancellationToken) is not null;
    }

    private async Task PushPendingCustomerDisplaysAsync(CancellationToken cancellationToken)
    {
        var pending = await _dbContext.SyncOutboxItems
            .Where(x => x.EntityType.StartsWith("CustomerDisplay:") && x.Status == "Pending")
            .OrderBy(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var outbox in pending)
        {
            outbox.Status = "InProgress";
            outbox.LastAttemptUtc = DateTime.UtcNow;
            outbox.RetryCount++;
            await _dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                var succeeded = outbox.EntityType switch
                {
                    "CustomerDisplay:Create" => await PushCustomerDisplayCreateAsync(outbox, cancellationToken),
                    "CustomerDisplay:Update" => await PushCustomerDisplayUpdateAsync(outbox, cancellationToken),
                    "CustomerDisplay:Delete" => await _syncApiClient.DeleteCustomerDisplayAsync(outbox.EntityId, cancellationToken, outbox.BaseVersion ?? 0),
                    _ => throw new InvalidOperationException($"Unknown customer display sync operation '{outbox.EntityType}'.")
                };

                outbox.Status = succeeded ? "Synced" : "Pending";
                outbox.SyncedAtUtc = succeeded ? DateTime.UtcNow : null;
                outbox.Error = succeeded ? null : "Server rejected customer display synchronization.";
            }
            catch (HttpRequestException ex)
            {
                outbox.Status = ex.StatusCode == System.Net.HttpStatusCode.Conflict ? "Conflict" : "Pending";
                outbox.SyncedAtUtc = null;
                outbox.Error = ex.Message;
                SetConflictDetails(outbox, ex);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                outbox.Status = "Pending";
                outbox.Error = "Customer display synchronization timed out.";
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> PushCustomerDisplayCreateAsync(SyncOutboxItem outbox, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<CreateCustomerDisplayRequest>(outbox.PayloadJson)
            ?? throw new InvalidOperationException("Invalid customer display create payload.");
        return await _syncApiClient.CreateCustomerDisplayAsync(request, cancellationToken) is not null;
    }

    private async Task<bool> PushCustomerDisplayUpdateAsync(SyncOutboxItem outbox, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<UpdateCustomerDisplayRequest>(outbox.PayloadJson)
            ?? throw new InvalidOperationException("Invalid customer display update payload.");
        request = request with { BaseVersion = (outbox.BaseVersion ?? 0) != 0 ? outbox.BaseVersion.GetValueOrDefault() : request.BaseVersion };
        return await _syncApiClient.UpdateCustomerDisplayAsync(request, cancellationToken) is not null;
    }

    private async Task PushPendingCashDesksAsync(CancellationToken cancellationToken)
    {
        var pending = await _dbContext.SyncOutboxItems
            .Where(x => x.EntityType.StartsWith("CashDesk:") && x.Status == "Pending")
            .OrderBy(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var outbox in pending)
        {
            outbox.Status = "InProgress";
            outbox.LastAttemptUtc = DateTime.UtcNow;
            outbox.RetryCount++;
            await _dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                var succeeded = outbox.EntityType switch
                {
                    "CashDesk:Create" => await PushCashDeskCreateAsync(outbox, cancellationToken),
                    "CashDesk:Update" => await PushCashDeskUpdateAsync(outbox, cancellationToken),
                    "CashDesk:Delete" => await _syncApiClient.DeleteCashDeskAsync(outbox.EntityId, cancellationToken, outbox.BaseVersion ?? 0),
                    _ => throw new InvalidOperationException($"Unknown cash desk sync operation '{outbox.EntityType}'.")
                };

                outbox.Status = succeeded ? "Synced" : "Pending";
                outbox.SyncedAtUtc = succeeded ? DateTime.UtcNow : null;
                outbox.Error = succeeded ? null : "Server rejected cash desk synchronization.";
            }
            catch (HttpRequestException ex)
            {
                outbox.Status = ex.StatusCode == System.Net.HttpStatusCode.Conflict ? "Conflict" : "Pending";
                outbox.SyncedAtUtc = null;
                outbox.Error = ex.Message;
                SetConflictDetails(outbox, ex);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                outbox.Status = "Pending";
                outbox.Error = "Cash desk synchronization timed out.";
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> PushCashDeskCreateAsync(
        SyncOutboxItem outbox,
        CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<CreateCashDeskRequest>(outbox.PayloadJson)
            ?? throw new InvalidOperationException("Invalid cash desk create payload.");
        return await _syncApiClient.CreateCashDeskAsync(request, cancellationToken) is not null;
    }

    private async Task<bool> PushCashDeskUpdateAsync(
        SyncOutboxItem outbox,
        CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<UpdateCashDeskRequest>(outbox.PayloadJson)
            ?? throw new InvalidOperationException("Invalid cash desk update payload.");
        request = request with { BaseVersion = (outbox.BaseVersion ?? 0) != 0 ? outbox.BaseVersion.GetValueOrDefault() : request.BaseVersion };
        return await _syncApiClient.UpdateCashDeskAsync(request, cancellationToken) is not null;
    }

    private async Task PushPendingPaymentTerminalsAsync(CancellationToken cancellationToken)
    {
        var pending = await _dbContext.SyncOutboxItems
            .Where(x => x.EntityType.StartsWith("PaymentTerminal:") && x.Status == "Pending")
            .OrderBy(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var outbox in pending)
        {
            outbox.Status = "InProgress";
            outbox.LastAttemptUtc = DateTime.UtcNow;
            outbox.RetryCount++;
            await _dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                var succeeded = outbox.EntityType switch
                {
                    "PaymentTerminal:Create" => await PushPaymentTerminalCreateAsync(outbox, cancellationToken),
                    "PaymentTerminal:Update" => await PushPaymentTerminalUpdateAsync(outbox, cancellationToken),
                    "PaymentTerminal:Delete" => await _syncApiClient.DeletePaymentTerminalAsync(outbox.EntityId, cancellationToken, outbox.BaseVersion ?? 0),
                    _ => throw new InvalidOperationException($"Unknown payment terminal sync operation '{outbox.EntityType}'.")
                };

                outbox.Status = succeeded ? "Synced" : "Pending";
                outbox.SyncedAtUtc = succeeded ? DateTime.UtcNow : null;
                outbox.Error = succeeded ? null : "Server rejected payment terminal synchronization.";
            }
            catch (HttpRequestException ex)
            {
                outbox.Status = ex.StatusCode == System.Net.HttpStatusCode.Conflict ? "Conflict" : "Pending";
                outbox.SyncedAtUtc = null;
                outbox.Error = ex.Message;
                SetConflictDetails(outbox, ex);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                outbox.Status = "Pending";
                outbox.Error = "Payment terminal synchronization timed out.";
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> PushPaymentTerminalCreateAsync(
        SyncOutboxItem outbox,
        CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<CreatePaymentTerminalRequest>(outbox.PayloadJson)
            ?? throw new InvalidOperationException("Invalid payment terminal create payload.");
        return await _syncApiClient.CreatePaymentTerminalAsync(request, cancellationToken) is not null;
    }

    private async Task<bool> PushPaymentTerminalUpdateAsync(
        SyncOutboxItem outbox,
        CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<UpdatePaymentTerminalRequest>(outbox.PayloadJson)
            ?? throw new InvalidOperationException("Invalid payment terminal update payload.");
        request = request with { BaseVersion = (outbox.BaseVersion ?? 0) != 0 ? outbox.BaseVersion.GetValueOrDefault() : request.BaseVersion };
        return await _syncApiClient.UpdatePaymentTerminalAsync(request, cancellationToken) is not null;
    }

    private async Task PushPendingStoresAsync(CancellationToken cancellationToken)
    {
        var pending = await _dbContext.SyncOutboxItems
            .Where(x => x.EntityType.StartsWith("Store:") && x.Status == "Pending")
            .OrderBy(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var outbox in pending)
        {
            outbox.Status = "InProgress";
            outbox.LastAttemptUtc = DateTime.UtcNow;
            outbox.RetryCount++;
            await _dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                var succeeded = outbox.EntityType switch
                {
                    "Store:Create" => await PushStoreCreateAsync(outbox, cancellationToken),
                    "Store:Update" => await PushStoreUpdateAsync(outbox, cancellationToken),
                    "Store:Delete" => await _syncApiClient.DeleteStoreAsync(outbox.EntityId, cancellationToken, outbox.BaseVersion ?? 0),
                    _ => throw new InvalidOperationException($"Unknown store sync operation '{outbox.EntityType}'.")
                };

                outbox.Status = succeeded ? "Synced" : "Pending";
                outbox.SyncedAtUtc = succeeded ? DateTime.UtcNow : null;
                outbox.Error = succeeded ? null : "Server rejected store synchronization.";
            }
            catch (HttpRequestException ex)
            {
                outbox.Status = ex.StatusCode == System.Net.HttpStatusCode.Conflict ? "Conflict" : "Pending";
                outbox.SyncedAtUtc = null;
                outbox.Error = ex.Message;
                SetConflictDetails(outbox, ex);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                outbox.Status = "Pending";
                outbox.Error = "Store synchronization timed out.";
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> PushStoreCreateAsync(
        SyncOutboxItem outbox,
        CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<CreateStoreRequest>(outbox.PayloadJson)
            ?? throw new InvalidOperationException("Invalid store create payload.");
        return await _syncApiClient.CreateStoreAsync(request, cancellationToken) is not null;
    }

    private async Task<bool> PushStoreUpdateAsync(
        SyncOutboxItem outbox,
        CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<UpdateStoreRequest>(outbox.PayloadJson)
            ?? throw new InvalidOperationException("Invalid store update payload.");
        request = request with { BaseVersion = (outbox.BaseVersion ?? 0) != 0 ? outbox.BaseVersion.GetValueOrDefault() : request.BaseVersion };
        return await _syncApiClient.UpdateStoreAsync(request, cancellationToken) is not null;
    }

    private async Task PushPendingMenusAsync(CancellationToken cancellationToken)
    {
        var pending = await _dbContext.SyncOutboxItems
            .Where(x => x.EntityType.StartsWith("Menu:") && x.Status == "Pending")
            .OrderBy(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var outbox in pending)
        {
            outbox.Status = "InProgress";
            outbox.LastAttemptUtc = DateTime.UtcNow;
            outbox.RetryCount++;
            await _dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                var succeeded = outbox.EntityType switch
                {
                    "Menu:Create" => await PushMenuCreateAsync(outbox, cancellationToken),
                    "Menu:Update" => await PushMenuUpdateAsync(outbox, cancellationToken),
                    "Menu:Delete" => await _syncApiClient.DeleteMenuAsync(outbox.EntityId, cancellationToken, outbox.BaseVersion ?? 0),
                    _ => throw new InvalidOperationException($"Unknown menu sync operation '{outbox.EntityType}'.")
                };

                outbox.Status = succeeded ? "Synced" : "Pending";
                outbox.SyncedAtUtc = succeeded ? DateTime.UtcNow : null;
                outbox.Error = succeeded ? null : "Server rejected menu synchronization.";
            }
            catch (HttpRequestException ex)
            {
                outbox.Status = ex.StatusCode == System.Net.HttpStatusCode.Conflict ? "Conflict" : "Pending";
                outbox.SyncedAtUtc = null;
                outbox.Error = ex.Message;
                SetConflictDetails(outbox, ex);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                outbox.Status = "Pending";
                outbox.Error = "Menu synchronization timed out.";
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> PushMenuCreateAsync(
        SyncOutboxItem outbox,
        CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<CreateMenuRequest>(outbox.PayloadJson)
            ?? throw new InvalidOperationException("Invalid menu create payload.");
        return await _syncApiClient.CreateMenuAsync(request, cancellationToken) is not null;
    }

    private async Task<bool> PushMenuUpdateAsync(
        SyncOutboxItem outbox,
        CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<UpdateMenuRequest>(outbox.PayloadJson)
            ?? throw new InvalidOperationException("Invalid menu update payload.");
        request = request with { BaseVersion = (outbox.BaseVersion ?? 0) != 0 ? outbox.BaseVersion.GetValueOrDefault() : request.BaseVersion };
        return await _syncApiClient.UpdateMenuAsync(request, cancellationToken) is not null;
    }

    private async Task PushPendingPaymentsAsync(SyncState syncState, CancellationToken cancellationToken)
    {
        var pending = await _dbContext.SyncOutboxItems
            .Where(x => x.EntityType == "Payment" && x.Status == "Pending")
            .OrderBy(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        var items = new List<PaymentSyncItemDto>(pending.Count);

        foreach (var outbox in pending)
        {
            var dto = JsonSerializer.Deserialize<PaymentSyncItemDto>(outbox.PayloadJson);
            if (dto is null)
            {
                outbox.Status = "Failed";
                outbox.Error = "Invalid payload JSON";
                continue;
            }

            items.Add(dto);
            outbox.Status = "InProgress";
            outbox.LastAttemptUtc = DateTime.UtcNow;
            outbox.RetryCount += 1;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (items.Count == 0)
        {
            return;
        }

        var request = new UploadPaymentsRequest(Environment.MachineName, items);
        var response = await _syncApiClient.UploadPaymentsAsync(request, cancellationToken);

        if (response is null)
        {
            foreach (var outbox in pending.Where(p => p.Status == "InProgress"))
            {
                outbox.Status = "Pending";
                outbox.Error = "Upload request failed";
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var resultMap = response.Results.ToDictionary(r => r.PaymentId, r => r);

        foreach (var outbox in pending.Where(p => p.Status == "InProgress"))
        {
            if (!resultMap.TryGetValue(outbox.EntityId, out var result))
            {
                outbox.Status = "Pending";
                outbox.Error = "Missing result from server";
                continue;
            }

            if (result.Status is "Accepted" or "Duplicate")
            {
                outbox.Status = "Synced";
                outbox.SyncedAtUtc = DateTime.UtcNow;
                outbox.Error = null;
            }
            else
            {
                outbox.Status = "Failed";
                outbox.Error = result.Message;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task PullEquipmentAsync(CancellationToken cancellationToken)
    {
        var pulls = new (string Name, Func<Task> Pull)[]
        {
            ("printers", async () => { await _printerService.RefreshAsync(null, cancellationToken); }),
            ("scanners", async () => { await _scannerService.RefreshAsync(null, cancellationToken); }),
            ("customer displays", async () => { await _customerDisplayService.RefreshAsync(null, cancellationToken); }),
            ("cash desks", async () => { await _cashDeskService.RefreshAsync(null, cancellationToken); }),
            ("payment terminals", async () => { await _paymentTerminalService.RefreshAsync(cancellationToken); }),
            ("stores", async () => { await _storeService.RefreshAsync(cancellationToken); }),
            ("menus", async () => { await _menuService.RefreshAsync(cancellationToken); })
        };

        foreach (var (name, pull) in pulls)
        {
            try
            {
                await pull();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to pull {EquipmentType}; continuing with other synchronization work.", name);
            }
        }
    }

    private static void SetConflictDetails(SyncOutboxItem outbox, HttpRequestException exception)
    {
        if (exception is not SyncConflictHttpException conflict)
        {
            return;
        }

        outbox.ConflictVersion = conflict.CurrentVersion;
        outbox.ConflictRecordJson = conflict.RecordJson;
    }

    private async Task PullProductsAsync(SyncState syncState, CancellationToken cancellationToken)
    {
        var response = await _syncApiClient.GetProductsDeltaAsync(syncState.LastProductsSyncUtc, cancellationToken);
        if (response is null)
        {
            return;
        }

        foreach (var item in response.Items)
        {
            var existing = await _dbContext.Products
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == item.Id, cancellationToken);

            if (existing is null)
            {
                existing = new Product
                {
                    Id = item.Id
                };

                await _dbContext.Products.AddAsync(existing, cancellationToken);
            }

            existing.Name = item.Name;
            existing.Barcode = item.Barcode;
            existing.SKU = item.SKU;
            existing.CostPrice = item.CostPrice;
            existing.SalePrice = item.SalePrice;
            existing.IsActive = item.IsActive;
            existing.IsDeleted = item.IsDeleted;
            existing.UpdatedAt = item.UpdatedAt;
            existing.TaxId = item.TaxId;
        }

        syncState.LastProductsSyncUtc = response.ServerTimeUtc;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<SyncState> GetOrCreateSyncStateAsync(CancellationToken cancellationToken)
    {
        var syncState = await _dbContext.SyncStates
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (syncState is not null)
        {
            return syncState;
        }

        syncState = new SyncState();
        await _dbContext.SyncStates.AddAsync(syncState, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return syncState;
    }
}