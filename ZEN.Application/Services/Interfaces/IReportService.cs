using ZEN.Application.DTOs.Reports;

namespace ZEN.Application.Services.Interfaces;

public interface IReportService
{
    Task<SalesReportDto> GetSalesReportAsync(SalesReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryReportDto>> GetInventoryReportAsync(Guid? warehouseId = null, bool? lowStockOnly = null, CancellationToken cancellationToken = default);
    Task<ShiftReportDto?> GetShiftReportAsync(Guid shiftId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShiftReportDto>> GetShiftsReportAsync(DateTime startDate, DateTime endDate, Guid? storeId = null, CancellationToken cancellationToken = default);
    Task<XReportDto?> GetXReportAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<XReportDto?> GetXReportDataAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<ZReportDto?> GetZReportAsync(Guid shiftId, decimal closingCash, string? notes = null, CancellationToken cancellationToken = default);
}