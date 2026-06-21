using Microsoft.AspNetCore.Mvc;
using ZEN.Application.DTOs.Inventory;
using ZEN.Application.Services.Interfaces;

namespace ZEN.ApiService.Endpoints;

public static class InventoryEndpoints
{
    public static RouteGroupBuilder MapInventoryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/warehouse/{warehouseId:guid}", async (Guid warehouseId, IInventoryService inventoryService, CancellationToken ct) =>
        {
            var items = await inventoryService.GetInventoryByWarehouseAsync(warehouseId, ct);
            return Results.Ok(items);
        })
        .WithName("GetInventoryByWarehouse")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/product/{productId:guid}/warehouse/{warehouseId:guid}", async (Guid productId, Guid warehouseId, IInventoryService inventoryService, CancellationToken ct) =>
        {
            var item = await inventoryService.GetInventoryByProductAsync(productId, warehouseId, ct);
            return item == null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName("GetInventoryByProduct")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/purchase", async ([FromBody] CreateStockPurchaseRequest request, IInventoryService inventoryService, CancellationToken ct) =>
        {
            var purchase = await inventoryService.CreateStockPurchaseAsync(request, ct);
            return Results.Created($"/api/inventory/purchases/{purchase.Id}", purchase);
        })
        .WithName("CreateStockPurchase")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/purchases", async (Guid? supplierId, IInventoryService inventoryService, CancellationToken ct) =>
        {
            var purchases = await inventoryService.GetStockPurchasesAsync(supplierId, ct);
            return Results.Ok(purchases);
        })
        .WithName("GetStockPurchases")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/low-stock/{warehouseId:guid}", async (Guid warehouseId, IInventoryService inventoryService, CancellationToken ct) =>
        {
            var items = await inventoryService.GetLowStockItemsAsync(warehouseId, ct);
            return Results.Ok(items);
        })
        .WithName("GetLowStockItems")
        .WithOpenApi()
        .RequireAuthorization();

        return group;
    }
}
