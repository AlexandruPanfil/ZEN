using Microsoft.AspNetCore.Mvc;
using ZEN.Application.DTOs.Sync;
using ZEN.Application.Services.Interfaces;

namespace ZEN.ApiService.Endpoints;

public static class SyncEndpoints
{
    public static RouteGroupBuilder MapSyncEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/products", async (
            [FromQuery] DateTime? sinceUtc,
            [FromQuery] int pageSize,
            ISyncService syncService,
            CancellationToken ct) =>
        {
            var response = await syncService.GetProductsDeltaAsync(sinceUtc, pageSize == 0 ? 500 : pageSize, ct);
            return Results.Ok(response);
        })
        .WithName("GetSyncProductsDelta")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/payments", async (
            [FromBody] UploadPaymentsRequest request,
            ISyncService syncService,
            CancellationToken ct) =>
        {
            var response = await syncService.UploadPaymentsAsync(request, ct);
            return Results.Ok(response);
        })
        .WithName("UploadSyncPayments")
        .WithOpenApi()
        .RequireAuthorization();

        return group;
    }
}