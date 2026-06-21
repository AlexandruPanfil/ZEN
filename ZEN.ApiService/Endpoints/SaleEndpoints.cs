using Microsoft.AspNetCore.Mvc;
using ZEN.Application.DTOs.Sales;
using ZEN.Application.Services.Interfaces;

namespace ZEN.ApiService.Endpoints;

public static class SaleEndpoints
{
    public static RouteGroupBuilder MapSaleEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", async ([FromBody] CreateSaleRequest request, ISaleService saleService, CancellationToken ct) =>
        {
            var sale = await saleService.CreateSaleAsync(request, ct);
            return Results.Created($"/api/sales/{sale.Id}", sale);
        })
        .WithName("CreateSale")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, ISaleService saleService, CancellationToken ct) =>
        {
            var sale = await saleService.GetByIdAsync(id, ct);
            return sale == null ? Results.NotFound() : Results.Ok(sale);
        })
        .WithName("GetSaleById")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/shift/{shiftId:guid}", async (Guid shiftId, ISaleService saleService, CancellationToken ct) =>
        {
            var sales = await saleService.GetByShiftAsync(shiftId, ct);
            return Results.Ok(sales);
        })
        .WithName("GetSalesByShift")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/hold", async ([FromBody] HoldSaleRequest request, ISaleService saleService, CancellationToken ct) =>
        {
            var heldSale = await saleService.HoldSaleAsync(request, ct);
            return Results.Created($"/api/sales/held/{heldSale.Id}", heldSale);
        })
        .WithName("HoldSale")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/held/{storeId:guid}", async (Guid storeId, ISaleService saleService, CancellationToken ct) =>
        {
            var heldSales = await saleService.GetHeldSalesAsync(storeId, ct);
            return Results.Ok(heldSales);
        })
        .WithName("GetHeldSales")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/resume/{heldSaleId:guid}", async (Guid heldSaleId, ISaleService saleService, CancellationToken ct) =>
        {
            var resumed = await saleService.ResumeHeldSaleAsync(heldSaleId, ct);
            return resumed ? Results.Ok() : Results.NotFound();
        })
        .WithName("ResumeSale")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/{id:guid}/void", async (Guid id, [FromBody] string reason, ISaleService saleService, CancellationToken ct) =>
        {
            var voided = await saleService.VoidSaleAsync(id, reason, ct);
            return voided ? Results.Ok() : Results.NotFound();
        })
        .WithName("VoidSale")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/{id:guid}/refund", async (Guid id, [FromBody] string reason, ISaleService saleService, CancellationToken ct) =>
        {
            var refunded = await saleService.RefundSaleAsync(id, reason, ct);
            return refunded ? Results.Ok() : Results.NotFound();
        })
        .WithName("RefundSale")
        .WithOpenApi()
        .RequireAuthorization();

        return group;
    }
}
