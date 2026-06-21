using ZEN.Application.DTOs.Reports;
using ZEN.Application.Services.Interfaces;

namespace ZEN.ApiService.Endpoints;

public static class ReportEndpoints
{
    public static RouteGroupBuilder MapReportEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/sales", async (DateTime startDate, DateTime endDate, Guid? storeId, IReportService reportService, CancellationToken ct) =>
        {
            var report = await reportService.GetSalesReportAsync(startDate, endDate, storeId, ct);
            return Results.Ok(report);
        })
        .WithName("GetSalesReport")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/inventory", async (Guid? warehouseId, bool? lowStockOnly, IReportService reportService, CancellationToken ct) =>
        {
            var report = await reportService.GetInventoryReportAsync(warehouseId, lowStockOnly, ct);
            return Results.Ok(report);
        })
        .WithName("GetInventoryReport")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/shift/{shiftId:guid}", async (Guid shiftId, IReportService reportService, CancellationToken ct) =>
        {
            var report = await reportService.GetShiftReportAsync(shiftId, ct);
            return report == null ? Results.NotFound() : Results.Ok(report);
        })
        .WithName("GetShiftReport")
        .WithOpenApi()
        .RequireAuthorization();

        return group;
    }
}
