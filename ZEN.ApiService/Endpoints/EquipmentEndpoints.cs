using Microsoft.AspNetCore.Mvc;
using ZEN.Application.DTOs.Admin;
using ZEN.Application.Services.Interfaces;

namespace ZEN.ApiService.Endpoints;

public static class EquipmentEndpoints
{
    public static RouteGroupBuilder MapEquipmentEndpoints(this RouteGroupBuilder group)
    {
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
        .RequireAuthorization();

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

        return group;
    }
}