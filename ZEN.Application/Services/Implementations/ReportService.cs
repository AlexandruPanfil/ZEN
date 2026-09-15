using ZEN.Application.DTOs.FiscalCloud;
using ZEN.Application.DTOs.Reports;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Enums;
using ZEN.Domain.Interfaces;
using System.Linq;

namespace ZEN.Application.Services.Implementations;

public class ReportService : IReportService
{
    private readonly IRepository<InventoryItem> _inventoryRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Shift> _shiftRepository;
    private readonly IRepository<Sale> _saleRepository;
    private readonly IRepository<SaleItem> _saleItemRepository;
    private readonly IRepository<SalePayment> _salePaymentRepository;
    private readonly IRepository<Printer> _printerRepository;
    private readonly IRepository<CashDesk> _cashDeskRepository;
    private readonly IRepository<Store> _storeRepository;
    private readonly IStoreContext _storeContext;
    private readonly IFiscalCloudService _fiscalCloudService;
    private readonly IUnitOfWork _unitOfWork;

    public ReportService(
        IRepository<InventoryItem> inventoryRepository,
        IRepository<Product> productRepository,
        IRepository<Shift> shiftRepository,
        IRepository<Sale> saleRepository,
        IRepository<SaleItem> saleItemRepository,
        IRepository<SalePayment> salePaymentRepository,
        IRepository<Printer> printerRepository,
        IRepository<CashDesk> cashDeskRepository,
        IRepository<Store> storeRepository,
        IStoreContext storeContext,
        IFiscalCloudService fiscalCloudService,
        IUnitOfWork unitOfWork)
    {
        _inventoryRepository = inventoryRepository;
        _productRepository = productRepository;
        _shiftRepository = shiftRepository;
        _saleRepository = saleRepository;
        _saleItemRepository = saleItemRepository;
        _salePaymentRepository = salePaymentRepository;
        _printerRepository = printerRepository;
        _cashDeskRepository = cashDeskRepository;
        _storeRepository = storeRepository;
        _storeContext = storeContext;
        _fiscalCloudService = fiscalCloudService;
        _unitOfWork = unitOfWork;
    }

    public async Task<SalesReportDto> GetSalesReportAsync(SalesReportFilterDto filter, CancellationToken cancellationToken = default)
    {
        var storeIds = filter.StoreIds ?? new List<Guid>();
        if (!storeIds.Any())
        {
            var currentStoreId = _storeContext.GetCurrentStoreId();
            if (currentStoreId.HasValue)
            {
                storeIds.Add(currentStoreId.Value);
            }
        }

        // Apply date filter
        var sales = await _saleRepository.FindAsync(
            s => s.Date >= filter.StartDate && s.Date <= filter.EndDate 
                && (!storeIds.Any() || storeIds.Contains(s.StoreId)),
            cancellationToken);

        // Apply cash desk filter
        var cashDeskIds = filter.CashDeskIds ?? new List<Guid>();
        if (cashDeskIds.Any())
        {
            sales = sales.Where(s => s.ShiftId.HasValue && cashDeskIds.Contains(s.ShiftId.Value)).ToList();
        }

        var saleIds = sales.Select(s => s.Id).ToList();

        // Get sale items
        var saleItems = await _saleItemRepository.FindAsync(
            i => saleIds.Contains(i.SaleId),
            cancellationToken);

        // Apply product filter
        var productIds = filter.ProductIds ?? new List<Guid>();
        if (productIds.Any())
        {
            saleItems = saleItems.Where(i => productIds.Contains(i.ProductId)).ToList();
        }

        var saleProductIds = saleItems.Select(i => i.ProductId).Distinct().ToList();
        var products = await _productRepository.FindAsync(
            p => saleProductIds.Contains(p.Id),
            cancellationToken);
        var productsDict = products.ToDictionary(p => p.Id);

        // Apply category filter
        var categoryIds = filter.CategoryIds ?? new List<Guid>();
        if (categoryIds.Any())
        {
            var filteredProductIds = products
                .Where(p => p.CategoryId.HasValue && categoryIds.Contains(p.CategoryId.Value))
                .Select(p => p.Id)
                .ToHashSet();
            
            saleItems = saleItems.Where(i => filteredProductIds.Contains(i.ProductId)).ToList();
        }

        // Calculate totals
        var totalSales = sales.Sum(s => s.TotalAmount);
        var totalTax = sales.Sum(s => s.TaxAmount);
        var totalDiscount = sales.Sum(s => s.DiscountAmount);
        var netSales = totalSales - totalDiscount;
        var transactionCount = sales.Count();

        // Top products
        var topProducts = saleItems
            .GroupBy(i => productsDict.GetValueOrDefault(i.ProductId)?.Name ?? "Unknown Product")
            .Select(g => new SalesReportItemDto(
                g.Key,
                g.Sum(i => i.Quantity),
                g.Sum(i => i.TotalPrice)))
            .OrderByDescending(p => p.TotalSales)
            .Take(10);

        // Build hierarchical breakdown
        var storeBreakdowns = await BuildStoreBreakdownsAsync(sales, saleItems, productsDict, storeIds, cancellationToken);

        return new SalesReportDto(
            filter.StartDate,
            filter.EndDate,
            totalSales,
            totalTax,
            totalDiscount,
            netSales,
            transactionCount,
            topProducts,
            storeBreakdowns);
    }

    private async Task<IEnumerable<StoreSalesBreakdownDto>> BuildStoreBreakdownsAsync(
        IEnumerable<Sale> sales,
        IEnumerable<SaleItem> saleItems,
        Dictionary<Guid, Product> productsDict,
        List<Guid> storeIds,
        CancellationToken cancellationToken)
    {
        var storeIdsToProcess = storeIds.Any() ? storeIds : sales.Select(s => s.StoreId).Distinct().ToList();
        var stores = await _storeRepository.FindAsync(s => storeIdsToProcess.Contains(s.Id), cancellationToken);
        var storesDict = stores.ToDictionary(s => s.Id);

        var cashDeskIds = sales.Where(s => s.ShiftId.HasValue).Select(s => s.ShiftId!.Value).Distinct().ToList();
        var cashDesks = await _cashDeskRepository.FindAsync(c => cashDeskIds.Contains(c.Id), cancellationToken);
        var cashDesksDict = cashDesks.ToDictionary(c => c.Id);

        var result = new List<StoreSalesBreakdownDto>();

        foreach (var storeId in storeIdsToProcess)
        {
            var storeSales = sales.Where(s => s.StoreId == storeId).ToList();
            var storeSaleIds = storeSales.Select(s => s.Id).ToList();
            var storeSaleItems = saleItems.Where(i => storeSaleIds.Contains(i.SaleId)).ToList();

            var storeTotalSales = storeSales.Sum(s => s.TotalAmount);
            var storeTransactionCount = storeSales.Count();

            var cashDeskBreakdowns = new List<CashDeskSalesBreakdownDto>();

            var storeCashDeskIds = storeSales.Where(s => s.ShiftId.HasValue).Select(s => s.ShiftId!.Value).Distinct().ToList();

            foreach (var cashDeskId in storeCashDeskIds)
            {
                var cashDeskSales = storeSales.Where(s => s.ShiftId.HasValue && s.ShiftId.Value == cashDeskId).ToList();
                var cashDeskSaleIds = cashDeskSales.Select(s => s.Id).ToList();
                var cashDeskSaleItems = storeSaleItems.Where(i => cashDeskSaleIds.Contains(i.SaleId)).ToList();

                var cashDeskTotalSales = cashDeskSales.Sum(s => s.TotalAmount);
                var cashDeskTransactionCount = cashDeskSales.Count();

                var productBreakdowns = cashDeskSaleItems
                    .GroupBy(i => i.ProductId)
                    .Select(g =>
                    {
                        var product = productsDict.GetValueOrDefault(g.Key);
                        return new ProductSalesBreakdownDto(
                            g.Key,
                            product?.Name ?? "Unknown Product",
                            product?.Barcode,
                            g.Sum(i => i.Quantity),
                            g.Sum(i => i.TotalPrice),
                            g.Sum(i => i.TaxAmount),
                            g.Sum(i => i.DiscountAmount));
                    })
                    .OrderByDescending(p => p.TotalSales)
                    .ToList();

                cashDeskBreakdowns.Add(new CashDeskSalesBreakdownDto(
                    cashDeskId,
                    cashDesksDict.GetValueOrDefault(cashDeskId)?.Name ?? "Unknown Cash Desk",
                    cashDeskTotalSales,
                    cashDeskTransactionCount,
                    productBreakdowns));
            }

            result.Add(new StoreSalesBreakdownDto(
                storeId,
                storesDict.GetValueOrDefault(storeId)?.Name ?? "Unknown Store",
                storeTotalSales,
                storeTransactionCount,
                cashDeskBreakdowns));
        }

        return result;
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

    public async Task<XReportDto?> GetXReportAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var shift = await _shiftRepository.FindFirstAsync(
            s => s.StoreId == storeId && s.Status == ShiftStatus.Open,
            cancellationToken);
        
        if (shift == null)
            return null;

        var sales = await _saleRepository.FindAsync(
            s => s.ShiftId.HasValue && s.ShiftId.Value == shift.Id,
            cancellationToken);

        var totalSales = sales.Sum(s => s.TotalAmount);
        var totalTax = sales.Sum(s => s.TaxAmount);
        var totalDiscount = sales.Sum(s => s.DiscountAmount);
        var netSales = totalSales - totalDiscount;
        var transactionCount = sales.Count();

        var payments = await _salePaymentRepository.FindAsync(
            sp => sp.Sale.ShiftId.HasValue && sp.Sale.ShiftId.Value == shift.Id,
            cancellationToken);

        var paymentSummary = payments
            .GroupBy(p => p.Method.ToString())
            .Select(g => new PaymentSummaryDto(
                g.Key,
                g.Sum(p => p.Amount),
                g.Count()))
            .ToList();

        var expectedCash = shift.OpeningCash + totalSales;

        var xReport = new XReportDto(
            shift.Id,
            $"SHIFT-{shift.Id.ToString("N")[..6].ToUpperInvariant()}",
            shift.OpenTime,
            shift.UserId,
            shift.OpeningCash,
            totalSales,
            totalTax,
            totalDiscount,
            netSales,
            transactionCount,
            paymentSummary,
            expectedCash);

        // Send intermediate totals to FiscalCloud for compliance
        var cashDesk = await _cashDeskRepository.FindFirstAsync(
            cd => cd.StoreId == storeId && cd.IsActive && cd.FiscalPrinterId.HasValue,
            cancellationToken);

        Printer? printer = null;
        if (cashDesk != null)
        {
            printer = await _printerRepository.GetByIdAsync(cashDesk.FiscalPrinterId.Value, cancellationToken);
        }

        if (printer != null && printer.IsActive && printer.Type == PrinterType.Fiscal)
        {
            try
            {
                var intermediateRequest = new FiscalCloudIntermediateTotalsRequestDto(
                    Id: Guid.NewGuid().ToString(),
                    PrintOnServer: true,
                    PrintFormMediaType: null);

                var fiscalSuccess = await _fiscalCloudService.SendIntermediateTotalsAsync(printer, intermediateRequest, cancellationToken);
                
                // X Report is non-fiscal, so we don't fail if FiscalCloud fails
                // Just log the result
                if (!fiscalSuccess)
                {
                    // Could log warning here, but don't fail the X report
                }
            }
            catch
            {
                // X Report is non-fiscal, so we don't fail if FiscalCloud fails
                // Just continue without fiscal integration
            }
        }

        return xReport;
    }

    public async Task<ZReportDto?> GetZReportAsync(Guid shiftId, decimal closingCash, string? notes = null, CancellationToken cancellationToken = default)
    {
        var shift = await _shiftRepository.GetByIdAsync(shiftId, cancellationToken);
        if (shift == null || shift.Status != ShiftStatus.Open)
            return null;

        // Get X report data first (but don't send to FiscalCloud again - we'll do it in Z report)
        var xReport = await GetXReportDataAsync(shift.StoreId, cancellationToken);
        if (xReport == null)
            return null;

        var expectedCash = xReport.ExpectedCash;
        var variance = closingCash - expectedCash;

        // Find fiscal printer through CashDesk for this store
        var cashDesk = await _cashDeskRepository.FindFirstAsync(
            cd => cd.StoreId == shift.StoreId && cd.IsActive && cd.FiscalPrinterId.HasValue,
            cancellationToken);

        Printer? printer = null;
        if (cashDesk != null)
        {
            printer = await _printerRepository.GetByIdAsync(cashDesk.FiscalPrinterId.Value, cancellationToken);
        }

        FiscalReceiptInfo fiscalReceipt;

        if (printer != null && printer.IsActive && printer.Type == PrinterType.Fiscal)
        {
            try
            {
                // Create Z report request for FiscalCloud
                var zRequest = new FiscalCloudCloseDayRequestDto(
                    Id: Guid.NewGuid().ToString(),
                    PrintOnServer: true,
                    PrintFormMediaType: null,
                    PdfPrintFormSettings: null,
                    ImagePrintFormSettings: null,
                    Print: true,
                    SendToEmail: false,
                    EmailAddress: null,
                    SendToPhone: false,
                    PhoneNumber: null,
                    AdditionalHeaderText: "",
                    AdditionalFooterText: $"Z Report - Shift: {shift.Id.ToString("N")[..8].ToUpperInvariant()}, Cashier: {shift.UserId}");

                var fiscalSuccess = await _fiscalCloudService.SendCloseDayAsync(printer, zRequest, cancellationToken);

                if (fiscalSuccess)
                {
                    var fiscalReceiptNumber = $"Z-{shift.Id.ToString("N")[..8].ToUpperInvariant()}-{DateTime.UtcNow:yyyyMMddHHmmss}";
                    fiscalReceipt = new FiscalReceiptInfo(
                        fiscalReceiptNumber,
                        DateTime.UtcNow,
                        true,
                        null);
                }
                else
                {
                    fiscalReceipt = new FiscalReceiptInfo(
                        $"Z-{shift.Id.ToString("N")[..8].ToUpperInvariant()}",
                        DateTime.UtcNow,
                        false,
                        "FiscalCloud close day operation failed");
                }
            }
            catch (Exception ex)
            {
                fiscalReceipt = new FiscalReceiptInfo(
                    $"Z-{shift.Id.ToString("N")[..8].ToUpperInvariant()}",
                    DateTime.UtcNow,
                    false,
                    ex.Message);
            }
        }
        else
        {
            fiscalReceipt = new FiscalReceiptInfo(
                $"Z-{shift.Id.ToString("N")[..8].ToUpperInvariant()}",
                DateTime.UtcNow,
                false,
                "No fiscal printer configured");
        }

        return new ZReportDto(xReport, closingCash, variance, fiscalReceipt);
    }

    public async Task<XReportDto?> GetXReportDataAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var shift = await _shiftRepository.FindFirstAsync(
            s => s.StoreId == storeId && s.Status == ShiftStatus.Open,
            cancellationToken);
        
        if (shift == null)
            return null;

        var sales = await _saleRepository.FindAsync(
            s => s.ShiftId.HasValue && s.ShiftId.Value == shift.Id,
            cancellationToken);

        var totalSales = sales.Sum(s => s.TotalAmount);
        var totalTax = sales.Sum(s => s.TaxAmount);
        var totalDiscount = sales.Sum(s => s.DiscountAmount);
        var netSales = totalSales - totalDiscount;
        var transactionCount = sales.Count();

        var payments = await _salePaymentRepository.FindAsync(
            sp => sp.Sale.ShiftId.HasValue && sp.Sale.ShiftId.Value == shift.Id,
            cancellationToken);

        var paymentSummary = payments
            .GroupBy(p => p.Method.ToString())
            .Select(g => new PaymentSummaryDto(
                g.Key,
                g.Sum(p => p.Amount),
                g.Count()))
            .ToList();

        var expectedCash = shift.OpeningCash + totalSales;

        return new XReportDto(
            shift.Id,
            $"SHIFT-{shift.Id.ToString("N")[..6].ToUpperInvariant()}",
            shift.OpenTime,
            shift.UserId,
            shift.OpeningCash,
            totalSales,
            totalTax,
            totalDiscount,
            netSales,
            transactionCount,
            paymentSummary,
            expectedCash);
    }
}