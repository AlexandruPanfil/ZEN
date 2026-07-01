using ZEN.Domain.Enums;

namespace ZEN.Application.DTOs.Inventory;

public record InventoryItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    Guid WarehouseId,
    string WarehouseName,
    decimal Quantity,
    decimal ReorderLevel,
    decimal? MaxStock);

public record PurchaseOrderDto(
    Guid Id,
    string Number,
    Guid SupplierId,
    string SupplierName,
    Guid WarehouseId,
    string WarehouseName,
    DateTime OrderDate,
    DateTime? ExpectedDate,
    PurchaseOrderStatus Status,
    decimal SubTotal,
    decimal TaxAmount,
    decimal TotalAmount,
    IEnumerable<PurchaseOrderItemDto> Items);

public record PurchaseOrderItemDto(
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal Total);

public record CreatePurchaseOrderRequest(
    Guid SupplierId,
    Guid WarehouseId,
    DateTime? ExpectedDate,
    string? Notes,
    IEnumerable<CreatePurchaseOrderItemRequest> Items);

public record CreatePurchaseOrderItemRequest(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice);

public record UpdatePurchaseOrderRequest(
    Guid Id,
    DateTime? ExpectedDate,
    string? Notes,
    PurchaseOrderStatus Status);

public record StockPurchaseDto(
    Guid Id,
    string Number,
    DateTime ReceiptDate,
    Guid SupplierId,
    string SupplierName,
    Guid WarehouseId,
    string WarehouseName,
    string? SupplierInvoiceNo,
    decimal SubTotal,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    StockPurchaseStatus Status,
    IEnumerable<StockPurchaseItemDto> Items);

public record StockPurchaseItemDto(
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitCost,
    decimal TaxRate,
    decimal Total);

public record CreateStockPurchaseRequest(
    Guid SupplierId,
    Guid WarehouseId,
    Guid? PurchaseOrderId,
    string? SupplierInvoiceNo,
    DateTime ReceiptDate,
    IEnumerable<CreateStockPurchaseItemRequest> Items,
    decimal PaidAmount);

public record CreateStockPurchaseItemRequest(
    Guid ProductId,
    decimal Quantity,
    decimal UnitCost,
    decimal TaxRate);

public record StockTransferDto(
    Guid Id,
    string Number,
    DateTime TransferDate,
    Guid SourceWarehouseId,
    string SourceWarehouseName,
    Guid DestinationWarehouseId,
    string DestinationWarehouseName,
    StockTransferStatus Status,
    IEnumerable<StockTransferItemDto> Items);

public record StockTransferItemDto(
    Guid ProductId,
    string ProductName,
    decimal Quantity);
