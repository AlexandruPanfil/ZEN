using ZEN.Application.DTOs.Suppliers;
using ZEN.Application.Services.Interfaces;

namespace ZEN.ApiService.Endpoints;

public static class SupplierEndpoints
{
    public static RouteGroupBuilder MapSupplierEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ISupplierService supplierService, CancellationToken ct) =>
        {
            var suppliers = await supplierService.GetAllAsync(ct);
            return Results.Ok(suppliers);
        })
        .WithName("GetAllSuppliers")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, ISupplierService supplierService, CancellationToken ct) =>
        {
            var supplier = await supplierService.GetByIdAsync(id, ct);
            return supplier == null ? Results.NotFound() : Results.Ok(supplier);
        })
        .WithName("GetSupplierById")
        .WithOpenApi()
        .RequireAuthorization();

        return group;
    }
}
