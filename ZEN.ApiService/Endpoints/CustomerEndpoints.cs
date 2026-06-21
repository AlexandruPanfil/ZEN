using ZEN.Application.DTOs.Customers;
using ZEN.Application.Services.Interfaces;

namespace ZEN.ApiService.Endpoints;

public static class CustomerEndpoints
{
    public static RouteGroupBuilder MapCustomerEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ICustomerService customerService, CancellationToken ct) =>
        {
            var customers = await customerService.GetAllAsync(ct);
            return Results.Ok(customers);
        })
        .WithName("GetAllCustomers")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, ICustomerService customerService, CancellationToken ct) =>
        {
            var customer = await customerService.GetByIdAsync(id, ct);
            return customer == null ? Results.NotFound() : Results.Ok(customer);
        })
        .WithName("GetCustomerById")
        .WithOpenApi()
        .RequireAuthorization();

        return group;
    }
}
