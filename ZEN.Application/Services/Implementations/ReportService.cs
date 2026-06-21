using ZEN.Application.DTOs.Reports;
using ZEN.Application.Services.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class ReportService : IReportService
{
    public Task<SalesReportDto> GetSalesReportAsync(DateTime startDate, DateTime endDate, Guid? storeId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<InventoryReportDto>> GetInventoryReportAsync(Guid? warehouseId = null, bool? lowStockOnly = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<ShiftReportDto?> GetShiftReportAsync(Guid shiftId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<ShiftReportDto>> GetShiftsReportAsync(DateTime startDate, DateTime endDate, Guid? storeId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
