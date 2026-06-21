using Microsoft.AspNetCore.Mvc;
using ZEN.Application.DTOs.Shifts;
using ZEN.Application.Services.Interfaces;

namespace ZEN.ApiService.Endpoints;

public static class ShiftEndpoints
{
    public static RouteGroupBuilder MapShiftEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/open", async ([FromBody] OpenShiftRequest request, IShiftService shiftService, CancellationToken ct) =>
        {
            var shift = await shiftService.OpenShiftAsync(request, ct);
            return Results.Created($"/api/shifts/{shift.Id}", shift);
        })
        .WithName("OpenShift")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/close", async ([FromBody] CloseShiftRequest request, IShiftService shiftService, CancellationToken ct) =>
        {
            var shift = await shiftService.CloseShiftAsync(request, ct);
            return Results.Ok(shift);
        })
        .WithName("CloseShift")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/current/{storeId:guid}", async (Guid storeId, IShiftService shiftService, CancellationToken ct) =>
        {
            var shift = await shiftService.GetCurrentShiftAsync(storeId, ct);
            return shift == null ? Results.NotFound() : Results.Ok(shift);
        })
        .WithName("GetCurrentShift")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/{shiftId:guid}", async (Guid shiftId, IShiftService shiftService, CancellationToken ct) =>
        {
            var shift = await shiftService.GetByIdAsync(shiftId, ct);
            return shift == null ? Results.NotFound() : Results.Ok(shift);
        })
        .WithName("GetShiftById")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/cash-movement", async ([FromBody] CreateCashMovementRequest request, IShiftService shiftService, CancellationToken ct) =>
        {
            var movement = await shiftService.CreateCashMovementAsync(request, ct);
            return Results.Created($"/api/shifts/cash-movements/{movement.Id}", movement);
        })
        .WithName("CreateCashMovement")
        .WithOpenApi()
        .RequireAuthorization();

        return group;
    }
}
