using Microsoft.AspNetCore.Mvc;
using ZEN.Application.Services.Interfaces;

namespace ZEN.ApiService.Endpoints;

public static class TaxEndpoints
{
    public static RouteGroupBuilder MapTaxEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ITaxService taxService, CancellationToken ct) =>
        {
            var taxes = await taxService.GetAllAsync(ct);
            return Results.Ok(taxes);
        })
        .WithName("GetAllTaxes")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/active", async (ITaxService taxService, CancellationToken ct) =>
        {
            var taxes = await taxService.GetActiveAsync(ct);
            return Results.Ok(taxes);
        })
        .WithName("GetActiveTaxes")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/default", async (ITaxService taxService, CancellationToken ct) =>
        {
            var defaultTax = await taxService.GetDefaultTaxAsync(ct);
            return defaultTax == null ? Results.NotFound() : Results.Ok(defaultTax);
        })
        .WithName("GetDefaultTax")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, ITaxService taxService, CancellationToken ct) =>
        {
            var tax = await taxService.GetByIdAsync(id, ct);
            return tax == null ? Results.NotFound() : Results.Ok(tax);
        })
        .WithName("GetTaxById")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/", async ([FromBody] CreateTaxRequest request, ITaxService taxService, CancellationToken ct) =>
        {
            var tax = await taxService.CreateAsync(request, ct);
            return Results.Created($"/api/taxes/{tax.Id}", tax);
        })
        .WithName("CreateTax")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateTaxRequest request, ITaxService taxService, CancellationToken ct) =>
        {
            if (id != request.Id)
                return Results.BadRequest("ID mismatch");

            try
            {
                var tax = await taxService.UpdateAsync(request, ct);
                return Results.Ok(tax);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("UpdateTax")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapDelete("/{id:guid}", async (Guid id, ITaxService taxService, CancellationToken ct) =>
        {
            var deleted = await taxService.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteTax")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPut("/{id:guid}/set-default", async (Guid id, ITaxService taxService, CancellationToken ct) =>
        {
            var success = await taxService.SetDefaultTaxAsync(id, ct);
            return success ? Results.Ok() : Results.NotFound();
        })
        .WithName("SetDefaultTax")
        .WithOpenApi()
        .RequireAuthorization();

        return group;
    }
}
