using Microsoft.AspNetCore.Mvc;
using ZEN.Application.DTOs.Products;
using ZEN.Application.Services.Interfaces;

namespace ZEN.ApiService.Endpoints;

public static class ProductGroupEndpoints
{
    public static RouteGroupBuilder MapProductGroupEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (IProductGroupService productGroupService, CancellationToken ct) =>
        {
            var productGroups = await productGroupService.GetAllAsync(ct);
            return Results.Ok(productGroups);
        })
        .WithName("GetAllProductGroups")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/active", async (IProductGroupService productGroupService, CancellationToken ct) =>
        {
            var productGroups = await productGroupService.GetActiveAsync(ct);
            return Results.Ok(productGroups);
        })
        .WithName("GetActiveProductGroups")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, IProductGroupService productGroupService, CancellationToken ct) =>
        {
            var productGroup = await productGroupService.GetByIdAsync(id, ct);
            return productGroup == null ? Results.NotFound() : Results.Ok(productGroup);
        })
        .WithName("GetProductGroupById")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/", async ([FromBody] CreateProductGroupRequest request, IProductGroupService productGroupService, CancellationToken ct) =>
        {
            var productGroup = await productGroupService.CreateAsync(request, ct);
            return Results.Created($"/api/products/groups/{productGroup.Id}", productGroup);
        })
        .WithName("CreateProductGroup")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateProductGroupRequest request, IProductGroupService productGroupService, CancellationToken ct) =>
        {
            if (id != request.Id)
                return Results.BadRequest("ID mismatch");

            try
            {
                var productGroup = await productGroupService.UpdateAsync(request, ct);
                return Results.Ok(productGroup);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("UpdateProductGroup")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/{id:guid}", async (Guid id, IProductGroupService productGroupService, CancellationToken ct) =>
        {
            var deleted = await productGroupService.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteProductGroup")
        .WithOpenApi()
        .RequireAuthorization();

        return group;
    }
}