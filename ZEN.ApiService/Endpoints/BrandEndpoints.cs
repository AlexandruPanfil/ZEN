using Microsoft.AspNetCore.Mvc;
using ZEN.Application.DTOs.Products;
using ZEN.Application.Services.Interfaces;

namespace ZEN.ApiService.Endpoints;

public static class BrandEndpoints
{
    public static RouteGroupBuilder MapBrandEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (IBrandService brandService, CancellationToken ct) =>
        {
            var brands = await brandService.GetAllAsync(ct);
            return Results.Ok(brands);
        })
        .WithName("GetAllBrands")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/active", async (IBrandService brandService, CancellationToken ct) =>
        {
            var brands = await brandService.GetActiveAsync(ct);
            return Results.Ok(brands);
        })
        .WithName("GetActiveBrands")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, IBrandService brandService, CancellationToken ct) =>
        {
            var brand = await brandService.GetByIdAsync(id, ct);
            return brand == null ? Results.NotFound() : Results.Ok(brand);
        })
        .WithName("GetBrandById")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/", async ([FromBody] CreateBrandRequest request, IBrandService brandService, CancellationToken ct) =>
        {
            var brand = await brandService.CreateAsync(request, ct);
            return Results.Created($"/api/brands/{brand.Id}", brand);
        })
        .WithName("CreateBrand")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateBrandRequest request, IBrandService brandService, CancellationToken ct) =>
        {
            if (id != request.Id)
                return Results.BadRequest("ID mismatch");

            try
            {
                var brand = await brandService.UpdateAsync(request, ct);
                return Results.Ok(brand);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("UpdateBrand")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/{id:guid}", async (Guid id, IBrandService brandService, CancellationToken ct) =>
        {
            var deleted = await brandService.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteBrand")
        .WithOpenApi()
        .RequireAuthorization();

        return group;
    }
}
