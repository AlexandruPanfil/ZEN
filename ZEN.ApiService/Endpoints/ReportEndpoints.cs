using Microsoft.AspNetCore.Mvc;
using ZEN.Application.DTOs.FiscalCloud;
using ZEN.Application.DTOs.Reports;
using ZEN.Application.DTOs.Shifts;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Enums;
using ZEN.Domain.Interfaces;

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

        group.MapGet("/x/{storeId:guid}", async (Guid storeId, IReportService reportService, CancellationToken ct) =>
        {
            var report = await reportService.GetXReportAsync(storeId, ct);
            return report == null ? Results.NotFound("No open shift found for this store") : Results.Ok(report);
        })
        .WithName("GetXReport")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapGet("/x-data/{storeId:guid}", async (Guid storeId, IReportService reportService, CancellationToken ct) =>
        {
            var report = await reportService.GetXReportDataAsync(storeId, ct);
            return report == null ? Results.NotFound("No open shift found for this store") : Results.Ok(report);
        })
        .WithName("GetXReportData")
        .WithOpenApi()
        .RequireAuthorization();

        group.MapPost("/z", async ([FromBody] CloseShiftWithReportRequest request, 
            IReportService reportService, 
            IShiftService shiftService, 
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            // Generate Z report with FiscalCloud integration
            var zReport = await reportService.GetZReportAsync(request.ShiftId, request.ClosingCash, request.Notes, ct);
            if (zReport == null)
                return Results.NotFound("Shift not found or already closed");

            // If fiscal failed, don't close the shift
            if (zReport.FiscalReceipt != null && !zReport.FiscalReceipt.Success)
            {
                return Results.BadRequest(new { 
                    error = "Fiscal receipt failed", 
                    details = zReport.FiscalReceipt.QrCodeBase64,
                    zReport = zReport
                });
            }

            // Fiscal succeeded - now close the shift
            var closeRequest = new CloseShiftRequest(request.ShiftId, request.ClosingCash);
            await shiftService.CloseShiftAsync(closeRequest, ct);
            // Note: CloseShiftAsync already calls SaveChangesAsync internally

            return Results.Ok(zReport);
        })
        .WithName("GetZReport")
        .WithOpenApi()
        .RequireAuthorization();

        return group;
    }
}
