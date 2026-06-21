namespace ZEN.Application.DTOs.Reports;

public record SalesReportDto(
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalSales,
    decimal TotalTax,
    decimal TotalDiscount,
    decimal NetSales,
    int TransactionCount,
    IEnumerable<SalesReportItemDto> TopProducts);

public record SalesReportItemDto(
    string ProductName,
    decimal Quantity,
    decimal TotalSales);

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
