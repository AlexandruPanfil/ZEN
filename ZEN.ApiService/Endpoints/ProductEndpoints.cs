using Microsoft.AspNetCore.Mvc;
using ZEN.Application.DTOs.Products;
using ZEN.Application.Services.Interfaces;

namespace ZEN.ApiService.Endpoints;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (IProductService productService, CancellationToken ct) =>
        {
            var products = await productService.GetAllAsync(null, ct);
            return Results.Ok(products);
        })
        .WithName("GetAllProducts")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, IProductService productService, CancellationToken ct) =>
        {
            var product = await productService.GetByIdAsync(id, ct);
            return product == null ? Results.NotFound() : Results.Ok(product);
        })
        .WithName("GetProductById")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/barcode/{barcode}", async (string barcode, IProductService productService, CancellationToken ct) =>
        {
            var product = await productService.GetByBarcodeAsync(barcode, null, ct);
            return product == null ? Results.NotFound() : Results.Ok(product);
        })
        .WithName("GetProductByBarcode")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/search/{searchTerm}", async (string searchTerm, IProductService productService, CancellationToken ct) =>
        {
            var products = await productService.SearchAsync(searchTerm, null, ct);
            return Results.Ok(products);
        })
        .WithName("SearchProducts")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/", async ([FromBody] CreateProductRequest request, IProductService productService, CancellationToken ct) =>
        {
            var product = await productService.CreateAsync(request, ct);
            return Results.Created($"/api/products/{product.Id}", product);
        })
        .WithName("CreateProduct")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateProductRequest request, IProductService productService, CancellationToken ct) =>
        {
            if (id != request.Id)
                return Results.BadRequest("ID mismatch");

            try
            {
                var product = await productService.UpdateAsync(request, ct);
                return Results.Ok(product);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("UpdateProduct")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/{id:guid}", async (Guid id, IProductService productService, CancellationToken ct) =>
        {
            var deleted = await productService.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteProduct")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/categories", async (IProductService productService, CancellationToken ct) =>
        {
            var categories = await productService.GetCategoriesAsync(ct);
            return Results.Ok(categories);
        })
        .WithName("GetCategories")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/groups", async (IProductService productService, CancellationToken ct) =>
        {
            var groups = await productService.GetProductGroupsAsync(ct);
            return Results.Ok(groups);
        })
        .WithName("GetProductGroups")
        .WithOpenApi()
        .RequireAuthorization();

        return group;
    }
}
