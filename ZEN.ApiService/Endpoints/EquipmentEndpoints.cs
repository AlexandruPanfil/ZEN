using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ZEN.Application.DTOs.Admin;
using ZEN.Application.DTOs.CashDeskMenu;
using ZEN.Application.Services.Interfaces;
using ZEN.ApiService.Hubs;
using ZEN.Domain.Entities;
using ZEN.Infrastructure.Persistence;

namespace ZEN.ApiService.Endpoints;

public static class EquipmentEndpoints
{
    public static RouteGroupBuilder MapEquipmentEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/stores", async (
            ZenDbContext dbContext,
            CancellationToken ct) =>
        {
            var stores = await dbContext.Stores
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .Select(s => new StoreDto(
                    s.Id,
                    s.Name,
                    s.Code,
                    s.Address,
                    s.City,
                    s.Phone,
                    s.Email,
                    s.IsActive))
                .ToListAsync(ct);
            return Results.Ok(stores);
        })
        .WithName("GetStores")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/stores", async (
            CreateStoreRequest request,
            ZenDbContext dbContext,
            CancellationToken ct) =>
        {
            var store = new Store
            {
                Id = request.Id ?? Guid.NewGuid(),
                Name = request.Name,
                Code = request.Code,
                Address = request.Address,
                City = request.City,
                Phone = request.Phone,
                Email = request.Email,
                IsActive = true
            };

            dbContext.Stores.Add(store);
            await dbContext.SaveChangesAsync(ct);
            return Results.Created($"/api/equipments/stores/{store.Id}", ToStoreDto(store));
        })
        .WithName("CreateStore")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPut("/stores/{id:guid}", async (
            Guid id,
            UpdateStoreRequest request,
            ZenDbContext dbContext,
            CancellationToken ct) =>
        {
            if (id != request.Id)
                return Results.BadRequest();

            var store = await dbContext.Stores.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (store is null)
                return Results.NotFound();

            store.Name = request.Name;
            store.Code = request.Code;
            store.Address = request.Address;
            store.City = request.City;
            store.Phone = request.Phone;
            store.Email = request.Email;
            store.IsActive = request.IsActive;
            await dbContext.SaveChangesAsync(ct);
            return Results.Ok(ToStoreDto(store));
        })
        .WithName("UpdateStore")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/stores/{id:guid}", async (
            Guid id,
            ZenDbContext dbContext,
            CancellationToken ct) =>
        {
            var store = await dbContext.Stores.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (store is null)
                return Results.NotFound();

            dbContext.Stores.Remove(store);
            await dbContext.SaveChangesAsync(ct);
            return Results.NoContent();
        })
        .WithName("DeleteStore")
        .WithOpenApi()
        .RequireAuthorization();

        // Printers
        group.MapGet("/printers", async (
            IPrinterService printerService,
            CancellationToken ct) =>
        {
            var printers = await printerService.GetAllAsync(ct);
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
            var printer = await printerService.UpdateAsync(request, ct);
            return Results.Ok(printer);
        })
        .WithName("UpdatePrinter")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/printers/{id:guid}", async (
            Guid id,
            IPrinterService printerService,
            CancellationToken ct) =>
        {
            var deleted = await printerService.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
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
            IPaymentTerminalService terminalService,
            CancellationToken ct) =>
        {
            var terminals = await terminalService.GetAllAsync(ct);
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
            var terminal = await terminalService.UpdateAsync(request, ct);
            return Results.Ok(terminal);
        })
        .WithName("UpdatePaymentTerminal")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/payment-terminals/{id:guid}", async (
            Guid id,
            IPaymentTerminalService terminalService,
            CancellationToken ct) =>
        {
            var deleted = await terminalService.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeletePaymentTerminal")
        .WithOpenApi()
        .RequireAuthorization();

        // Scanners
        group.MapGet("/scanners", async (
            IScannerService scannerService,
            CancellationToken ct) =>
        {
            var scanners = await scannerService.GetAllAsync(ct);
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
            var scanner = await scannerService.UpdateAsync(request, ct);
            return Results.Ok(scanner);
        })
        .WithName("UpdateScanner")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/scanners/{id:guid}", async (
            Guid id,
            IScannerService scannerService,
            CancellationToken ct) =>
        {
            var deleted = await scannerService.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteScanner")
        .WithOpenApi()
        .RequireAuthorization();

        // Customer Displays
        group.MapGet("/customer-displays", async (
            ICustomerDisplayService displayService,
            CancellationToken ct) =>
        {
            var displays = await displayService.GetAllAsync(ct);
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
            var display = await displayService.UpdateAsync(request, ct);
            return Results.Ok(display);
        })
        .WithName("UpdateCustomerDisplay")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/customer-displays/{id:guid}", async (
            Guid id,
            ICustomerDisplayService displayService,
            CancellationToken ct) =>
        {
            var deleted = await displayService.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteCustomerDisplay")
        .WithOpenApi()
        .RequireAuthorization();

        // Cash Desks
        group.MapGet("/cash-desks", async (
            [FromQuery] Guid? storeId,
            ICashDeskService cashDeskService,
            CancellationToken ct) =>
        {
            var cashDesks = storeId.HasValue
                ? await cashDeskService.GetAllAsync(storeId.Value, ct)
                : await cashDeskService.GetAllAsync(cancellationToken: ct);
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
            var cashDesk = await cashDeskService.UpdateAsync(request, ct);
            return Results.Ok(cashDesk);
        })
        .WithName("UpdateCashDesk")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/cash-desks/{id:guid}", async (
            Guid id,
            ICashDeskService cashDeskService,
            CancellationToken ct) =>
        {
            var deleted = await cashDeskService.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
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
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var menus = await menuService.GetAllSummariesAsync(ct);
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
            var menu = await menuService.UpdateAsync(request, ct);
            return Results.Ok(menu);
        })
        .WithName("UpdateMenu")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/menus/{id:guid}", async (
            Guid id,
            IMenuService menuService,
            CancellationToken ct) =>
        {
            var deleted = await menuService.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
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
            var deleted = await menuService.RemoveMenuGroupAsync(menuGroupId, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RemoveMenuGroup")
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

    private static StoreDto ToStoreDto(Store store) => new(
        store.Id,
        store.Name,
        store.Code,
        store.Address,
        store.City,
        store.Phone,
        store.Email,
        store.IsActive);
}