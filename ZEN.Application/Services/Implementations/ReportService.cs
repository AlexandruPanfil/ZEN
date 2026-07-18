using ZEN.Application.DTOs.Reports;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Enums;
using ZEN.Domain.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class ReportService : IReportService
{
    private readonly IRepository<InventoryItem> _inventoryRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Shift> _shiftRepository;
    private readonly IRepository<Sale> _saleRepository;
    private readonly IRepository<SaleItem> _saleItemRepository;
    private readonly IStoreContext _storeContext;

    public ReportService(
        IRepository<InventoryItem> inventoryRepository,
        IRepository<Product> productRepository,
        IRepository<Shift> shiftRepository,
        IRepository<Sale> saleRepository,
        IRepository<SaleItem> saleItemRepository,
        IStoreContext storeContext)
    {
        _inventoryRepository = inventoryRepository;
        _productRepository = productRepository;
        _shiftRepository = shiftRepository;
        _saleRepository = saleRepository;
        _saleItemRepository = saleItemRepository;
        _storeContext = storeContext;
    }

    public async Task<SalesReportDto> GetSalesReportAsync(DateTime startDate, DateTime endDate, Guid? storeId = null, CancellationToken cancellationToken = default)
    {
        var storeIdFromContext = storeId ?? _storeContext.GetCurrentStoreId();

        var sales = await _saleRepository.FindAsync(
            s => s.Date >= startDate && s.Date <= endDate && (!storeIdFromContext.HasValue || s.StoreId == storeIdFromContext.Value),
            cancellationToken);

        var saleIds = sales.Select(s => s.Id).ToList();
        var saleItems = await _saleItemRepository.FindAsync(
            i => saleIds.Contains(i.SaleId),
            cancellationToken);

        var productIds = saleItems.Select(i => i.ProductId).Distinct().ToList();
        var products = await _productRepository.FindAsync(
            p => productIds.Contains(p.Id),
            cancellationToken);
        var productsDict = products.ToDictionary(p => p.Id);

        var topProducts = saleItems
            .GroupBy(i => productsDict.GetValueOrDefault(i.ProductId)?.Name ?? "Unknown Product")
            .Select(g => new SalesReportItemDto(
                g.Key,
                g.Sum(i => i.Quantity),
                g.Sum(i => i.TotalPrice)))
            .OrderByDescending(p => p.TotalSales)
            .Take(10);

        return new SalesReportDto(
            startDate,
            endDate,
            sales.Sum(s => s.TotalAmount),
            sales.Sum(s => s.TaxAmount),
            sales.Sum(s => s.DiscountAmount),
            sales.Sum(s => s.TotalAmount) - sales.Sum(s => s.DiscountAmount),
            sales.Count(),
            topProducts);
    }

    public async Task<IEnumerable<InventoryReportDto>> GetInventoryReportAsync(Guid? warehouseId = null, bool? lowStockOnly = null, CancellationToken cancellationToken = default)
    {
        var items = await _inventoryRepository.FindAsync(
            i => !warehouseId.HasValue || i.WarehouseId == warehouseId.Value,
            cancellationToken);

        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _productRepository.FindAsync(
            p => productIds.Contains(p.Id),
            cancellationToken);
        var productsDict = products.ToDictionary(p => p.Id);

        var result = new List<InventoryReportDto>();

        foreach (var item in items)
        {
            var product = productsDict.GetValueOrDefault(item.ProductId);
            var belowReorder = item.Quantity <= item.MinQuantity;
            if (lowStockOnly == true && !belowReorder)
                continue;

            result.Add(new InventoryReportDto(
                item.ProductId,
                product?.Name ?? "Unknown Product",
                product?.Barcode,
                item.Quantity,
                item.MinQuantity,
                belowReorder,
                item.Quantity * (product?.CostPrice ?? 0)));
        }

        return result;
    }

    public async Task<ShiftReportDto?> GetShiftReportAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        var shift = await _shiftRepository.GetByIdAsync(shiftId, cancellationToken);
        if (shift == null)
            return null;

        var sales = await _saleRepository.FindAsync(s => s.ShiftId.HasValue && s.ShiftId.Value == shiftId, cancellationToken);
        var totalSales = sales.Sum(s => s.TotalAmount);
        var salesCount = sales.Count();
        var expectedCash = shift.OpeningCash + totalSales;
        var variance = shift.Status == ShiftStatus.Closed ? shift.ClosingCash - expectedCash : (decimal?)null;

        return new ShiftReportDto(
            $"SHIFT-{shift.Id.ToString("N")[..6].ToUpperInvariant()}",
            shift.OpenTime,
            shift.CloseTime,
            "Unknown",
            shift.OpeningCash,
            shift.Status == ShiftStatus.Closed ? shift.ClosingCash : null,
            totalSales,
            salesCount,
            variance);
    }

    public async Task<IEnumerable<ShiftReportDto>> GetShiftsReportAsync(DateTime startDate, DateTime endDate, Guid? storeId = null, CancellationToken cancellationToken = default)
    {
        var storeIdFromContext = storeId ?? _storeContext.GetCurrentStoreId();

        var shifts = await _shiftRepository.FindAsync(
            s => s.OpenTime >= startDate && s.OpenTime <= endDate && (!storeIdFromContext.HasValue || s.StoreId == storeIdFromContext.Value),
            cancellationToken);

        var shiftIds = shifts.Select(s => s.Id).ToList();
        var sales = await _saleRepository.FindAsync(
            s => s.ShiftId.HasValue && shiftIds.Contains(s.ShiftId.Value),
            cancellationToken);
        var salesByShift = sales.Where(s => s.ShiftId.HasValue)
            .GroupBy(s => s.ShiftId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = shifts.Select(shift =>
        {
            var shiftSales = salesByShift.GetValueOrDefault(shift.Id, new List<Sale>());
            var totalSales = shiftSales.Sum(s => s.TotalAmount);
            var variance = shift.Status == ShiftStatus.Closed ? shift.ClosingCash - (shift.OpeningCash + totalSales) : (decimal?)null;

            return new ShiftReportDto(
                $"SHIFT-{shift.Id.ToString("N")[..6].ToUpperInvariant()}",
                shift.OpenTime,
                shift.CloseTime,
                "Unknown",
                shift.OpeningCash,
                shift.Status == ShiftStatus.Closed ? shift.ClosingCash : null,
                totalSales,
                shiftSales.Count(),
                variance);
        });

        return result;
    }
}
