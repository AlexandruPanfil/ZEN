using Microsoft.AspNetCore.Mvc;
using ZEN.Application.DTOs.Products;
using ZEN.Application.Services.Interfaces;

namespace ZEN.ApiService.Endpoints;

public static class CategoryEndpoints
{
    public static RouteGroupBuilder MapCategoryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ICategoryService categoryService, CancellationToken ct) =>
        {
            var categories = await categoryService.GetAllAsync(ct);
            return Results.Ok(categories);
        })
        .WithName("GetAllCategories")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/active", async (ICategoryService categoryService, CancellationToken ct) =>
        {
            var categories = await categoryService.GetActiveAsync(ct);
            return Results.Ok(categories);
        })
        .WithName("GetActiveCategories")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, ICategoryService categoryService, CancellationToken ct) =>
        {
            var category = await categoryService.GetByIdAsync(id, ct);
            return category == null ? Results.NotFound() : Results.Ok(category);
        })
        .WithName("GetCategoryById")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/", async ([FromBody] CreateCategoryRequest request, ICategoryService categoryService, CancellationToken ct) =>
        {
            var category = await categoryService.CreateAsync(request, ct);
            return Results.Created($"/api/categories/{category.Id}", category);
        })
        .WithName("CreateCategory")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateCategoryRequest request, ICategoryService categoryService, CancellationToken ct) =>
        {
            if (id != request.Id)
                return Results.BadRequest("ID mismatch");

            try
            {
                var category = await categoryService.UpdateAsync(request, ct);
                return Results.Ok(category);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("UpdateCategory")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/{id:guid}", async (Guid id, ICategoryService categoryService, CancellationToken ct) =>
        {
            var deleted = await categoryService.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteCategory")
        .WithOpenApi()
        .RequireAuthorization();

        return group;
    }
}
