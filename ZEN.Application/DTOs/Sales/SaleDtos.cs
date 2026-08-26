using ZEN.Domain.Enums;

namespace ZEN.Application.DTOs.Sales;

public record SaleDto(
    Guid Id,
    string Number,
    DateTime Date,
    Guid? CustomerId,
    string? CustomerName,
    decimal SubTotal,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal ChangeAmount,
    SaleStatus Status,
    bool? FiscalReceiptSent,
    string? FiscalReceiptError,
    IEnumerable<SaleItemDto> Items,
    IEnumerable<SalePaymentDto> Payments);

public record SaleItemDto(
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal TaxRate,
    decimal Total);

public record SalePaymentDto(
    PaymentMethod Method,
    decimal Amount,
    DateTime PaidAt);

public record CreateSaleRequest(
    Guid? CustomerId,
    Guid StoreId,
    Guid? ShiftId,
    Guid? CashDeskId,
    IEnumerable<CreateSaleItemRequest> Items,
    IEnumerable<CreateSalePaymentRequest> Payments,
    decimal? LoyaltyPointsRedeemed);

public record CreateSaleItemRequest(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal? TaxRate);

public record CreateSalePaymentRequest(
    PaymentMethod Method,
    decimal Amount);

public record HoldSaleRequest(
    Guid StoreId,
    Guid? CustomerId,
    string SerializedItems);

public record HeldSaleDto(
    Guid Id,
    DateTime HeldAt,
    string? CustomerName,
    string SerializedItems);
