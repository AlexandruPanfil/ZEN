namespace ZEN.Application.DTOs.Reports;

public record SalesReportFilterDto(
    DateTime StartDate,
    DateTime EndDate,
    List<Guid>? StoreIds = null,
    List<Guid>? CashDeskIds = null,
    List<Guid>? ProductIds = null,
    List<Guid>? CategoryIds = null,
    List<Guid>? GroupIds = null);

public record SalesReportDto(
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalSales,
    decimal TotalTax,
    decimal TotalDiscount,
    decimal NetSales,
    int TransactionCount,
    IEnumerable<SalesReportItemDto> TopProducts,
    IEnumerable<StoreSalesBreakdownDto> StoreBreakdowns);

public record SalesReportItemDto(
    string ProductName,
    decimal Quantity,
    decimal TotalSales);

public record StoreSalesBreakdownDto(
    Guid StoreId,
    string StoreName,
    decimal TotalSales,
    int TransactionCount,
    IEnumerable<CashDeskSalesBreakdownDto> CashDeskBreakdowns);

public record CashDeskSalesBreakdownDto(
    Guid CashDeskId,
    string CashDeskName,
    decimal TotalSales,
    int TransactionCount,
    IEnumerable<ProductSalesBreakdownDto> ProductBreakdowns);

public record ProductSalesBreakdownDto(
    Guid ProductId,
    string ProductName,
    string? Barcode,
    decimal QuantitySold,
    decimal TotalSales,
    decimal TotalTax,
    decimal TotalDiscount);

public record InventoryReportDto(
    Guid ProductId,
    string ProductName,
    string? Barcode,
    decimal TotalStock,
    decimal ReorderLevel,
    bool BelowReorderLevel,
    decimal StockValue);

public record ShiftReportDto(
    string ShiftNumber,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    string UserName,
    decimal OpeningCash,
    decimal? ClosingCash,
    decimal TotalSales,
    int SalesCount,
    decimal? Variance);

public record XReportDto(
    Guid ShiftId,
    string ShiftNumber,
    DateTime OpenedAt,
    string Cashier,
    decimal OpeningCash,
    decimal TotalSales,
    decimal TotalTax,
    decimal TotalDiscount,
    decimal NetSales,
    int TransactionCount,
    IEnumerable<PaymentSummaryDto> Payments,
    decimal ExpectedCash);

public record PaymentSummaryDto(
    string Method,
    decimal Amount,
    int Count);

public record ZReportDto(
    XReportDto XReportData,
    decimal ClosingCash,
    decimal Variance,
    FiscalReceiptInfo? FiscalReceipt);

public record FiscalReceiptInfo(
    string Number,
    DateTime Date,
    bool Success,
    string? QrCodeBase64);

public record CloseShiftWithReportRequest(
    Guid ShiftId,
    decimal ClosingCash,
    string? Notes);