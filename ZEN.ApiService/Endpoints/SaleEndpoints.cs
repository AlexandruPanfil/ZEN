using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ZEN.Application.DTOs.Sales;
using ZEN.Application.Services.Interfaces;
using ZEN.ApiService.Hubs;

namespace ZEN.ApiService.Endpoints;

public static class SaleEndpoints
{
    public static RouteGroupBuilder MapSaleEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", async ([FromBody] CreateSaleRequest request, ISaleService saleService, ILogger<SaleDto> logger, CancellationToken ct) =>
        {
            try
            {
                logger.LogInformation("Creating sale - StoreId: {StoreId}, CashDeskId: {CashDeskId}, Items: {ItemCount}", 
                    request.StoreId, request.CashDeskId, request.Items.Count());

                var sale = await saleService.CreateSaleAsync(request, ct);

                logger.LogInformation("Sale created successfully - Id: {SaleId}, Number: {Number}", sale.Id, sale.Number);

                return Results.Created($"/api/sales/{sale.Id}", sale);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Invalid sale request: {Message}", ex.Message);
                return Results.Problem(
                    title: "Invalid sale request",
                    detail: ex.Message,
                    statusCode: 400);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating sale: {Message}", ex.Message);
                return Results.Problem(
                    title: "Error creating sale",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("CreateSale")
        .WithOpenApi()
        .AllowAnonymous(); // Temporarily allow anonymous for debugging

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

        group.MapPost("/hold", async ([FromBody] HoldSaleRequest request, ISaleService saleService, IHubContext<CashDeskHub> hubContext, CancellationToken ct) =>
        {
            var heldSale = await saleService.HoldSaleAsync(request, ct);
            
            // Broadcast held sale created
            await hubContext.Clients.Group($"store_{request.StoreId}").SendAsync("HeldSaleCreated", new
            {
                HeldSaleId = heldSale.Id,
                StoreId = request.StoreId,
                CustomerName = heldSale.CustomerName,
                SerializedItems = heldSale.SerializedItems,
                ItemCount = heldSale.SerializedItems != null ? System.Text.Json.JsonSerializer.Deserialize<List<object>>(heldSale.SerializedItems)?.Count ?? 0 : 0,
                HeldAt = heldSale.HeldAt
            }, ct);
            
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

        group.MapPost("/held/{heldSaleId:guid}/complete", async (Guid heldSaleId, [FromBody] string completedBy, ISaleService saleService, IHubContext<CashDeskHub> hubContext, CancellationToken ct) =>
        {
            // First get the held sale to know the store ID
            var heldSales = await saleService.GetHeldSalesAsync(Guid.Empty, ct);
            var heldSale = heldSales.FirstOrDefault(h => h.Id == heldSaleId);
            
            var completed = await saleService.CompleteHeldSaleAsync(heldSaleId, completedBy, ct);
            
            if (completed && heldSale != null)
            {
                // Broadcast held sale completed with store ID
                await hubContext.Clients.Group($"store_{heldSale.StoreId}").SendAsync("HeldSaleCompleted", new
                {
                    HeldSaleId = heldSaleId,
                    StoreId = heldSale.StoreId,
                    CompletedBy = completedBy,
                    CompletedAt = DateTime.UtcNow
                }, ct);
            }
            else if (completed)
            {
                // Fallback: broadcast to all stores
                await hubContext.Clients.All.SendAsync("HeldSaleCompleted", new
                {
                    HeldSaleId = heldSaleId,
                    CompletedBy = completedBy,
                    CompletedAt = DateTime.UtcNow
                }, ct);
            }
            
            return completed ? Results.Ok() : Results.NotFound();
        })
        .WithName("CompleteHeldSale")
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
