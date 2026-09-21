using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ZEN.Application.DTOs.Admin;
using ZEN.Application.DTOs.CashDeskMenu;
using ZEN.Application.Services.Interfaces;
using ZEN.ApiService.Hubs;
using ZEN.Domain.Entities;

namespace ZEN.ApiService.Endpoints;

public static class EquipmentEndpoints
{
    public static RouteGroupBuilder MapEquipmentEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/stores", async (
            IStoreService storeService,
            CancellationToken ct) =>
        {
            var stores = await storeService.GetAllAsync(ct, includeDeleted: true);
            return Results.Ok(stores);
        })
        .WithName("GetStores")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/stores", async (
            [FromBody] CreateStoreRequest request,
            IStoreService storeService,
            CancellationToken ct) =>
        {
            var store = await storeService.CreateAsync(request, ct);
            return Results.Created($"/api/equipments/stores/{store.Id}", store);
        })
        .WithName("CreateStore")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPut("/stores/{id:guid}", async (
            Guid id,
            [FromBody] UpdateStoreRequest request,
            IStoreService storeService,
            CancellationToken ct) =>
        {
            if (id != request.Id)
                return Results.BadRequest();

            try
            {
                var store = await storeService.UpdateAsync(request, ct);
                return Results.Ok(store);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (ZEN.Application.Services.Exceptions.SyncConflictException ex)
            {
                var record = await storeService.GetByIdAsync(ex.EntityId, ct);
                return Results.Conflict(new { ex.EntityId, ex.ExpectedVersion, ex.CurrentVersion, Record = record });
            }
        })
        .WithName("UpdateStore")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/stores/{id:guid}", async (
            Guid id,
            [FromQuery] long baseVersion,
            IStoreService storeService,
            CancellationToken ct) =>
        {
            try
            {
                var deleted = await storeService.DeleteAsync(id, ct, baseVersion);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (ZEN.Application.Services.Exceptions.SyncConflictException ex)
            {
                var record = await storeService.GetByIdAsync(ex.EntityId, ct);
                return Results.Conflict(new { ex.EntityId, ex.ExpectedVersion, ex.CurrentVersion, Record = record });
            }
        })
        .WithName("DeleteStore")
        .WithOpenApi()
        .RequireAuthorization();

        // Printers
        group.MapGet("/printers", async (
            [FromQuery] bool includeDeleted,
            IPrinterService printerService,
            CancellationToken ct) =>
        {
            var printers = await printerService.GetAllAsync(ct, includeDeleted);
            return Results.Ok(printers);
        })
        .WithName("GetPrinters")
        .WithOpenApi()
        .AllowAnonymous(); // Temporarily allow anonymous for debugging

        group.MapGet("/printers/fiscal", async (
            IPrinterService printerService,
            CancellationToken ct) =>
        {
            var printers = await printerService.GetFiscalPrintersAsync(ct);
            return Results.Ok(printers);
        })
        .WithName("GetFiscalPrinters")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/printers/{id:guid}", async (
            Guid id,
            IPrinterService printerService,
            CancellationToken ct) =>
        {
            var printer = await printerService.GetByIdAsync(id, ct);
            return printer is not null ? Results.Ok(printer) : Results.NotFound();
        })
        .WithName("GetPrinter")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/printers", async (
            [FromBody] CreatePrinterRequest request,
            IPrinterService printerService,
            CancellationToken ct) =>
        {
            var printer = await printerService.CreateAsync(request, ct);
            return Results.Created($"/api/equipments/printers/{printer.Id}", printer);
        })
        .WithName("CreatePrinter")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPut("/printers/{id:guid}", async (
            Guid id,
            [FromBody] UpdatePrinterRequest request,
            IPrinterService printerService,
            CancellationToken ct) =>
        {
            if (id != request.Id) return Results.BadRequest();

            try
            {
                var printer = await printerService.UpdateAsync(request, ct);
                return Results.Ok(printer);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (ZEN.Application.Services.Exceptions.SyncConflictException ex)
            {
                return Results.Conflict(new { ex.EntityId, ex.ExpectedVersion, ex.CurrentVersion, Record = (await printerService.GetAllAsync(ct, includeDeleted: true)).FirstOrDefault(x => x.Id == ex.EntityId) });
            }
        })
        .WithName("UpdatePrinter")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/printers/{id:guid}", async (
            Guid id,
            [FromQuery] long baseVersion,
            IPrinterService printerService,
            CancellationToken ct) =>
        {
            try
            {
                var deleted = await printerService.DeleteAsync(id, ct, baseVersion);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (ZEN.Application.Services.Exceptions.SyncConflictException ex)
            {
                return Results.Conflict(new { ex.EntityId, ex.ExpectedVersion, ex.CurrentVersion, Record = (await printerService.GetAllAsync(ct, includeDeleted: true)).FirstOrDefault(x => x.Id == ex.EntityId) });
            }
        })
        .WithName("DeletePrinter")
        .WithOpenApi()
        .RequireAuthorization();

        // Test FiscalCloud connection
        group.MapPost("/printers/{id:guid}/test-fiscalcloud", async (
            Guid id,
            IPrinterService printerService,
            IFiscalCloudService fiscalCloudService,
            CancellationToken ct) =>
        {
            var printerDto = await printerService.GetByIdAsync(id, ct);
            if (printerDto == null)
                return Results.NotFound(new { Message = "Printer not found" });

            if (string.IsNullOrEmpty(printerDto.ApiKey) ||
                string.IsNullOrEmpty(printerDto.ApiSecret) ||
                string.IsNullOrEmpty(printerDto.DeviceId))
                return Results.BadRequest(new { Message = "FiscalCloud credentials not configured" });

            // Convert DTO to entity for service call
            var printer = new ZEN.Domain.Entities.Printer
            {
                Id = printerDto.Id,
                ApiKey = printerDto.ApiKey,
                ApiSecret = printerDto.ApiSecret,
                DeviceId = printerDto.DeviceId,
                PointOfSaleId = printerDto.PointOfSaleId,
                FiscalCloudBaseUrl = printerDto.FiscalCloudBaseUrl
            };

            var status = await fiscalCloudService.GetStatusAsync(printer, ct);
            return Results.Ok(new
            {
                Success = status.IsOnline && status.IsConnected,
                Status = status
            });
        })
        .WithName("TestFiscalCloudConnection")
        .WithOpenApi()
        .RequireAuthorization();

        // Payment Terminals
        group.MapGet("/payment-terminals", async (
            [FromQuery] bool includeDeleted,
            IPaymentTerminalService terminalService,
            CancellationToken ct) =>
        {
            var terminals = await terminalService.GetAllAsync(ct, includeDeleted);
            return Results.Ok(terminals);
        })
        .WithName("GetPaymentTerminals")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/payment-terminals/{id:guid}", async (
            Guid id,
            IPaymentTerminalService terminalService,
            CancellationToken ct) =>
        {
            var terminal = await terminalService.GetByIdAsync(id, ct);
            return terminal is not null ? Results.Ok(terminal) : Results.NotFound();
        })
        .WithName("GetPaymentTerminal")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/payment-terminals", async (
            [FromBody] CreatePaymentTerminalRequest request,
            IPaymentTerminalService terminalService,
            CancellationToken ct) =>
        {
            var terminal = await terminalService.CreateAsync(request, ct);
            return Results.Created($"/api/equipments/payment-terminals/{terminal.Id}", terminal);
        })
        .WithName("CreatePaymentTerminal")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPut("/payment-terminals/{id:guid}", async (
            Guid id,
            [FromBody] UpdatePaymentTerminalRequest request,
            IPaymentTerminalService terminalService,
            CancellationToken ct) =>
        {
            if (id != request.Id) return Results.BadRequest();
            try
            {
                var terminal = await terminalService.UpdateAsync(request, ct);
                return Results.Ok(terminal);
            }
            catch (ZEN.Application.Services.Exceptions.SyncConflictException ex)
            {
                return Results.Conflict(new { ex.EntityId, ex.ExpectedVersion, ex.CurrentVersion, Record = (await terminalService.GetAllAsync(ct, includeDeleted: true)).FirstOrDefault(x => x.Id == ex.EntityId) });
            }
        })
        .WithName("UpdatePaymentTerminal")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/payment-terminals/{id:guid}", async (
            Guid id,
            [FromQuery] long baseVersion,
            IPaymentTerminalService terminalService,
            CancellationToken ct) =>
        {
            try
            {
                var deleted = await terminalService.DeleteAsync(id, ct, baseVersion);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (ZEN.Application.Services.Exceptions.SyncConflictException ex)
            {
                return Results.Conflict(new { ex.EntityId, ex.ExpectedVersion, ex.CurrentVersion, Record = (await terminalService.GetAllAsync(ct, includeDeleted: true)).FirstOrDefault(x => x.Id == ex.EntityId) });
            }
        })
        .WithName("DeletePaymentTerminal")
        .WithOpenApi()
        .RequireAuthorization();

        // Scanners
        group.MapGet("/scanners", async (
            [FromQuery] bool includeDeleted,
            IScannerService scannerService,
            CancellationToken ct) =>
        {
            var scanners = await scannerService.GetAllAsync(ct, includeDeleted);
            return Results.Ok(scanners);
        })
        .WithName("GetScanners")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/scanners/{id:guid}", async (
            Guid id,
            IScannerService scannerService,
            CancellationToken ct) =>
        {
            var scanner = await scannerService.GetByIdAsync(id, ct);
            return scanner is not null ? Results.Ok(scanner) : Results.NotFound();
        })
        .WithName("GetScanner")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/scanners", async (
            [FromBody] CreateScannerRequest request,
            IScannerService scannerService,
            CancellationToken ct) =>
        {
            var scanner = await scannerService.CreateAsync(request, ct);
            return Results.Created($"/api/equipments/scanners/{scanner.Id}", scanner);
        })
        .WithName("CreateScanner")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPut("/scanners/{id:guid}", async (
            Guid id,
            [FromBody] UpdateScannerRequest request,
            IScannerService scannerService,
            CancellationToken ct) =>
        {
            if (id != request.Id) return Results.BadRequest();
            try
            {
                var scanner = await scannerService.UpdateAsync(request, ct);
                return Results.Ok(scanner);
            }
            catch (ZEN.Application.Services.Exceptions.SyncConflictException ex)
            {
                return Results.Conflict(new { ex.EntityId, ex.ExpectedVersion, ex.CurrentVersion, Record = (await scannerService.GetAllAsync(ct, includeDeleted: true)).FirstOrDefault(x => x.Id == ex.EntityId) });
            }
        })
        .WithName("UpdateScanner")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/scanners/{id:guid}", async (
            Guid id,
            [FromQuery] long baseVersion,
            IScannerService scannerService,
            CancellationToken ct) =>
        {
            try
            {
                var deleted = await scannerService.DeleteAsync(id, ct, baseVersion);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (ZEN.Application.Services.Exceptions.SyncConflictException ex)
            {
                return Results.Conflict(new { ex.EntityId, ex.ExpectedVersion, ex.CurrentVersion, Record = (await scannerService.GetAllAsync(ct, includeDeleted: true)).FirstOrDefault(x => x.Id == ex.EntityId) });
            }
        })
        .WithName("DeleteScanner")
        .WithOpenApi()
        .RequireAuthorization();

        // Customer Displays
        group.MapGet("/customer-displays", async (
            [FromQuery] bool includeDeleted,
            ICustomerDisplayService displayService,
            CancellationToken ct) =>
        {
            var displays = await displayService.GetAllAsync(ct, includeDeleted);
            return Results.Ok(displays);
        })
        .WithName("GetCustomerDisplays")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/customer-displays/{id:guid}", async (
            Guid id,
            ICustomerDisplayService displayService,
            CancellationToken ct) =>
        {
            var display = await displayService.GetByIdAsync(id, ct);
            return display is not null ? Results.Ok(display) : Results.NotFound();
        })
        .WithName("GetCustomerDisplay")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/customer-displays", async (
            [FromBody] CreateCustomerDisplayRequest request,
            ICustomerDisplayService displayService,
            CancellationToken ct) =>
        {
            var display = await displayService.CreateAsync(request, ct);
            return Results.Created($"/api/equipments/customer-displays/{display.Id}", display);
        })
        .WithName("CreateCustomerDisplay")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPut("/customer-displays/{id:guid}", async (
            Guid id,
            [FromBody] UpdateCustomerDisplayRequest request,
            ICustomerDisplayService displayService,
            CancellationToken ct) =>
        {
            if (id != request.Id) return Results.BadRequest();
            try
            {
                var display = await displayService.UpdateAsync(request, ct);
                return Results.Ok(display);
            }
            catch (ZEN.Application.Services.Exceptions.SyncConflictException ex)
            {
                return Results.Conflict(new { ex.EntityId, ex.ExpectedVersion, ex.CurrentVersion, Record = (await displayService.GetAllAsync(ct, includeDeleted: true)).FirstOrDefault(x => x.Id == ex.EntityId) });
            }
        })
        .WithName("UpdateCustomerDisplay")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/customer-displays/{id:guid}", async (
            Guid id,
            [FromQuery] long baseVersion,
            ICustomerDisplayService displayService,
            CancellationToken ct) =>
        {
            try
            {
                var deleted = await displayService.DeleteAsync(id, ct, baseVersion);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (ZEN.Application.Services.Exceptions.SyncConflictException ex)
            {
                return Results.Conflict(new { ex.EntityId, ex.ExpectedVersion, ex.CurrentVersion, Record = (await displayService.GetAllAsync(ct, includeDeleted: true)).FirstOrDefault(x => x.Id == ex.EntityId) });
            }
        })
        .WithName("DeleteCustomerDisplay")
        .WithOpenApi()
        .RequireAuthorization();

        // Cash Desks
        group.MapGet("/cash-desks", async (
            [FromQuery] Guid? storeId,
            [FromQuery] bool includeDeleted,
            ICashDeskService cashDeskService,
            CancellationToken ct) =>
        {
            var cashDesks = storeId.HasValue
                ? await cashDeskService.GetAllAsync(storeId.Value, ct, includeDeleted)
                : await cashDeskService.GetAllAsync(cancellationToken: ct, includeDeleted: includeDeleted);
            return Results.Ok(cashDesks);
        })
        .WithName("GetCashDesks")
        .WithOpenApi()
        .RequireAuthorization();

        // Cash Desks with Status (for POS monitoring)
        group.MapGet("/cash-desks/status", async (
            [FromQuery] Guid? storeId,
            ICashDeskService cashDeskService,
            CancellationToken ct) =>
        {
            var cashDesks = await cashDeskService.GetAllWithStatusAsync(storeId, ct);
            return Results.Ok(cashDesks);
        })
        .WithName("GetCashDesksWithStatus")
        .WithOpenApi()
        .RequireAuthorization();

        // Cash Desk Heartbeat
        group.MapPost("/cash-desks/{id:guid}/heartbeat", async (
            Guid id,
            ICashDeskService cashDeskService,
            IHubContext<CashDeskHub> hubContext,
            CancellationToken ct) =>
        {
            var cashDesk = await cashDeskService.HeartbeatAsync(id, ct);
            if (cashDesk is not null)
            {
                // Broadcast cash desk status to store group
                await hubContext.Clients.Group($"store_{cashDesk.StoreId}").SendAsync("CashDeskStatusChanged", new
                {
                    CashDeskId = cashDesk.Id,
                    CashDeskName = cashDesk.Name,
                    StoreId = cashDesk.StoreId,
                    IsOnline = true,
                    LastActivityUtc = cashDesk.LastActivityUtc
                }, ct);
                return Results.Ok(cashDesk);
            }
            return Results.NotFound();
        })
        .WithName("CashDeskHeartbeat")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/cash-desks/{id:guid}", async (
            Guid id,
            ICashDeskService cashDeskService,
            CancellationToken ct) =>
        {
            var cashDesk = await cashDeskService.GetDetailByIdAsync(id, ct);
            return cashDesk is not null ? Results.Ok(cashDesk) : Results.NotFound();
        })
        .WithName("GetCashDesk")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/cash-desks", async (
            [FromBody] CreateCashDeskRequest request,
            ICashDeskService cashDeskService,
            CancellationToken ct) =>
        {
            var cashDesk = await cashDeskService.CreateAsync(request, ct);
            return Results.Created($"/api/equipments/cash-desks/{cashDesk.Id}", cashDesk);
        })
        .WithName("CreateCashDesk")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPut("/cash-desks/{id:guid}", async (
            Guid id,
            [FromBody] UpdateCashDeskRequest request,
            ICashDeskService cashDeskService,
            CancellationToken ct) =>
        {
            if (id != request.Id) return Results.BadRequest();
            try
            {
                var cashDesk = await cashDeskService.UpdateAsync(request, ct);
                return Results.Ok(cashDesk);
            }
            catch (ZEN.Application.Services.Exceptions.SyncConflictException ex)
            {
                return Results.Conflict(new { ex.EntityId, ex.ExpectedVersion, ex.CurrentVersion, Record = (await cashDeskService.GetAllAsync(cancellationToken: ct, includeDeleted: true)).FirstOrDefault(x => x.Id == ex.EntityId) });
            }
        })
        .WithName("UpdateCashDesk")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/cash-desks/{id:guid}", async (
            Guid id,
            [FromQuery] long baseVersion,
            ICashDeskService cashDeskService,
            CancellationToken ct) =>
        {
            try
            {
                var deleted = await cashDeskService.DeleteAsync(id, ct, baseVersion);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (ZEN.Application.Services.Exceptions.SyncConflictException ex)
            {
                return Results.Conflict(new { ex.EntityId, ex.ExpectedVersion, ex.CurrentVersion, Record = (await cashDeskService.GetAllAsync(cancellationToken: ct, includeDeleted: true)).FirstOrDefault(x => x.Id == ex.EntityId) });
            }
        })
        .WithName("DeleteCashDesk")
        .WithOpenApi()
        .RequireAuthorization();

        // Diagnostic endpoint to check FiscalCloud configuration
        group.MapGet("/cash-desks/{id:guid}/fiscal-status", async (
            Guid id,
            ICashDeskService cashDeskService,
            IPrinterService printerService,
            CancellationToken ct) =>
        {
            var cashDesk = await cashDeskService.GetDetailByIdAsync(id, ct);
            if (cashDesk is null)
                return Results.NotFound(new { Error = "Cash desk not found" });

            if (!cashDesk.FiscalPrinterId.HasValue)
                return Results.Ok(new 
                { 
                    CashDeskId = id,
                    CashDeskName = cashDesk.Name,
                    HasFiscalPrinter = false,
                    Message = "No fiscal printer attached to this cash desk"
                });

            var printer = await printerService.GetByIdAsync(cashDesk.FiscalPrinterId.Value, ct);
            if (printer is null)
                return Results.Ok(new 
                { 
                    CashDeskId = id,
                    CashDeskName = cashDesk.Name,
                    HasFiscalPrinter = true,
                    PrinterId = cashDesk.FiscalPrinterId.Value,
                    PrinterFound = false,
                    Message = "Fiscal printer not found"
                });

return Results.Ok(new 
            { 
                CashDeskId = id,
                CashDeskName = cashDesk.Name,
                HasFiscalPrinter = true,
                PrinterId = printer.Id,
                PrinterName = printer.Name,
                PrinterType = printer.FiscalDeviceType,
                PrinterFound = true,
                HasApiKey = !string.IsNullOrEmpty(printer.ApiKey),
                HasApiSecret = !string.IsNullOrEmpty(printer.ApiSecret),
                HasDeviceId = !string.IsNullOrEmpty(printer.DeviceId),
                IsFullyConfigured = !string.IsNullOrEmpty(printer.ApiKey) && 
                                   !string.IsNullOrEmpty(printer.ApiSecret) && 
                                   !string.IsNullOrEmpty(printer.DeviceId),
                Message = (!string.IsNullOrEmpty(printer.ApiKey) && 
                          !string.IsNullOrEmpty(printer.ApiSecret) && 
                          !string.IsNullOrEmpty(printer.DeviceId))
                      ? "Fiscal printer is fully configured for FiscalCloud"
                      : "Fiscal printer is missing required credentials (ApiKey, ApiSecret, or DeviceId)"
            });
        })
        .WithName("GetCashDeskFiscalStatus")
        .WithOpenApi()
        .AllowAnonymous(); // Allow anonymous for diagnostic purposes

        // Assign Menu to Cash Desk
        group.MapPost("/cash-desks/{id:guid}/menu", async (
            Guid id,
            [FromBody] AssignMenuToCashDeskRequest request,
            ICashDeskService cashDeskService,
            CancellationToken ct) =>
        {
            if (id != request.CashDeskId) return Results.BadRequest();
            var cashDesk = await cashDeskService.AssignMenuAsync(request, ct);
            return Results.Ok(cashDesk);
        })
        .WithName("AssignMenuToCashDesk")
        .WithOpenApi()
        .RequireAuthorization();

        // Menus
        group.MapGet("/menus", async (
            [FromQuery] bool includeDeleted,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var menus = await menuService.GetAllSummariesAsync(ct, includeDeleted);
            return Results.Ok(menus);
        })
        .WithName("GetMenus")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/menus/{id:guid}", async (
            Guid id,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var menu = await menuService.GetByIdAsync(id, ct);
            return menu is not null ? Results.Ok(menu) : Results.NotFound();
        })
        .WithName("GetMenu")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/menus", async (
            [FromBody] CreateMenuRequest request,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var menu = await menuService.CreateAsync(request, ct);
            return Results.Created($"/api/equipments/menus/{menu.Id}", menu);
        })
        .WithName("CreateMenu")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPut("/menus/{id:guid}", async (
            Guid id,
            [FromBody] UpdateMenuRequest request,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            if (id != request.Id) return Results.BadRequest();
            try
            {
                var menu = await menuService.UpdateAsync(request, ct);
                return Results.Ok(menu);
            }
            catch (ZEN.Application.Services.Exceptions.SyncConflictException ex)
            {
                return Results.Conflict(new { ex.EntityId, ex.ExpectedVersion, ex.CurrentVersion, Record = (await menuService.GetAllSummariesAsync(ct, includeDeleted: true)).FirstOrDefault(x => x.Id == ex.EntityId) });
            }
        })
        .WithName("UpdateMenu")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/menus/{id:guid}", async (
            Guid id,
            [FromQuery] long baseVersion,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            try
            {
                var deleted = await menuService.DeleteAsync(id, ct, baseVersion);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (ZEN.Application.Services.Exceptions.SyncConflictException ex)
            {
                return Results.Conflict(new { ex.EntityId, ex.ExpectedVersion, ex.CurrentVersion, Record = (await menuService.GetAllSummariesAsync(ct, includeDeleted: true)).FirstOrDefault(x => x.Id == ex.EntityId) });
            }
        })
        .WithName("DeleteMenu")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/menus/{id:guid}/items", async (
            Guid id,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var items = await menuService.GetFlatItemsAsync(id, ct);
            return Results.Ok(items);
        })
        .WithName("GetMenuItems")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/menus/{id:guid}/products", async (
            Guid id,
            [FromBody] AddProductToMenuRequest request,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var requestWithMenuId = request with { MenuId = id };
            var menu = await menuService.AddProductAsync(requestWithMenuId, ct);
            return Results.Ok(menu);
        })
        .WithName("AddProductToMenu")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/menus/products/{menuProductId:guid}", async (
            Guid menuProductId,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var deleted = await menuService.RemoveMenuProductAsync(menuProductId, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RemoveMenuProduct")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/menus/{id:guid}/groups", async (
            Guid id,
            [FromBody] AddGroupToMenuRequest request,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var requestWithMenuId = request with { MenuId = id };
            var menu = await menuService.AddGroupAsync(requestWithMenuId, ct);
            return Results.Ok(menu);
        })
        .WithName("AddGroupToMenu")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/menus/groups/{menuGroupId:guid}", async (
            Guid menuGroupId,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var deleted = await menuService.RemoveMenuGroupReferenceAsync(menuGroupId, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RemoveMenuGroupReference")
        .WithOpenApi()
        .RequireAuthorization();

        // Menu Groups (new independent groups)
        group.MapGet("/menus/{id:guid}/structure", async (
            Guid id,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var menu = await menuService.GetByIdAsync(id, ct);
            return menu is not null ? Results.Ok(menu) : Results.NotFound();
        })
        .WithName("GetMenuStructure")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/menus/{id:guid}/menu-groups", async (
            Guid id,
            [FromBody] CreateMenuGroupRequest request,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var requestWithMenuId = request with { MenuId = id };
            var menu = await menuService.AddMenuGroupAsync(requestWithMenuId, ct);
            return Results.Ok(menu);
        })
        .WithName("AddMenuGroup")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPut("/menu-groups/{id:guid}", async (
            Guid id,
            [FromBody] UpdateMenuGroupRequest request,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            if (id != request.Id) return Results.BadRequest();
            var menu = await menuService.UpdateMenuGroupAsync(request, ct);
            return Results.Ok(menu);
        })
        .WithName("UpdateMenuGroup")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/menu-groups/{id:guid}", async (
            Guid id,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var deleted = await menuService.RemoveMenuGroupAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RemoveMenuGroup")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/menu-groups/{id:guid}/products", async (
            Guid id,
            [FromBody] AddProductToMenuGroupRequest request,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var requestWithGroupId = request with { MenuGroupId = id };
            var menu = await menuService.AddProductToMenuGroupAsync(requestWithGroupId, ct);
            return Results.Ok(menu);
        })
        .WithName("AddProductToMenuGroup")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/menu-groups/{menuGroupId:guid}/products/{menuGroupProductId:guid}", async (
            Guid menuGroupId,
            Guid menuGroupProductId,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var deleted = await menuService.RemoveProductFromMenuGroupAsync(menuGroupProductId, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RemoveProductFromMenuGroup")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPut("/menus/{id:guid}/reorder-groups", async (
            Guid id,
            [FromBody] ReorderMenuGroupsRequest request,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var requestWithMenuId = request with { MenuId = id };
            var menu = await menuService.ReorderMenuGroupsAsync(requestWithMenuId, ct);
            return Results.Ok(menu);
        })
        .WithName("ReorderMenuGroups")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPut("/menu-groups/{id:guid}/reorder-products", async (
            Guid id,
            [FromBody] ReorderMenuGroupProductsRequest request,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var requestWithGroupId = request with { MenuGroupId = id };
            var menu = await menuService.ReorderMenuGroupProductsAsync(requestWithGroupId, ct);
            return Results.Ok(menu);
        })
        .WithName("ReorderMenuGroupProducts")
        .WithOpenApi()
        .RequireAuthorization();

        // Groups
        group.MapGet("/groups", async (
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var groups = await menuService.GetGroupsAsync(ct);
            return Results.Ok(groups);
        })
        .WithName("GetGroups")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/groups", async (
            [FromBody] CreateGroupRequest request,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var group = await menuService.CreateGroupAsync(request, ct);
            return Results.Created($"/api/equipments/groups/{group.Id}", group);
        })
        .WithName("CreateGroup")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPut("/groups/{id:guid}", async (
            Guid id,
            [FromBody] UpdateGroupRequest request,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            if (id != request.Id) return Results.BadRequest();
            var group = await menuService.UpdateGroupAsync(request, ct);
            return Results.Ok(group);
        })
        .WithName("UpdateGroup")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/groups/{id:guid}", async (
            Guid id,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var deleted = await menuService.DeleteGroupAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteGroup")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/groups/{groupId:guid}/products", async (
            Guid groupId,
            [FromBody] AddProductToGroupRequest request,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var requestWithGroupId = request with { GroupId = groupId };
            var success = await menuService.AddProductToGroupAsync(requestWithGroupId, ct);
            return success ? Results.Ok() : Results.BadRequest();
        })
        .WithName("AddProductToGroup")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/groups/{groupId:guid}/products/{productInGroupId:guid}", async (
            Guid groupId,
            Guid productInGroupId,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var deleted = await menuService.RemoveProductFromGroupAsync(productInGroupId, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RemoveProductFromGroup")
        .WithOpenApi()
        .RequireAuthorization();

        // Cash Desk menu items (resolved by cash desk for POS)
        group.MapGet("/cash-desks/{id:guid}/menu-items", async (
            Guid id,
            ICashDeskService cashDeskService,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var cashDesk = await cashDeskService.GetByIdAsync(id, ct);
            if (cashDesk == null) return Results.NotFound();
            if (!cashDesk.MenuId.HasValue) return Results.Ok(Enumerable.Empty<FlatMenuItemDto>());
            var items = await menuService.GetFlatItemsAsync(cashDesk.MenuId.Value, ct);
            return Results.Ok(items);
        })
        .WithName("GetCashDeskMenuItems")
        .WithOpenApi();

        // Fiscal Receipts
        group.MapGet("/fiscal-receipts", async (
            [FromQuery] Guid? saleId,
            IFiscalReceiptService receiptService,
            CancellationToken ct) =>
        {
            var receipts = await receiptService.GetAllAsync(saleId, ct);
            return Results.Ok(receipts);
        })
        .WithName("GetFiscalReceipts")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/fiscal-receipts/{id:guid}", async (
            Guid id,
            IFiscalReceiptService receiptService,
            CancellationToken ct) =>
        {
            var receipt = await receiptService.GetByIdAsync(id, ct);
            return receipt is not null ? Results.Ok(receipt) : Results.NotFound();
        })
        .WithName("GetFiscalReceipt")
        .WithOpenApi()
        .RequireAuthorization();

        return group;
    }
}