using ZEN.Domain.Enums;

namespace ZEN.Application.DTOs.Sales;

public record SellOrderDto(
    Guid Id,
    string Number,
    Guid CustomerId,
    string CustomerName,
    Guid StoreId,
    string StoreName,
    DateTime OrderDate,
    DateTime? ExpectedDate,
    SellOrderStatus Status,
    decimal SubTotal,
    decimal TaxAmount,
    decimal TotalAmount,
    IEnumerable<SellOrderItemDto> Items);

public record SellOrderItemDto(
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal TaxRate,
    decimal Total);

public record CreateSellOrderRequest(
    Guid CustomerId,
    Guid StoreId,
    DateTime? ExpectedDate,
    string? Notes,
    IEnumerable<CreateSellOrderItemRequest> Items);

public record CreateSellOrderItemRequest(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount = 0,
    decimal? TaxRate = null);

public record UpdateSellOrderRequest(
    Guid Id,
    DateTime? ExpectedDate,
    string? Notes,
    SellOrderStatus Status);

public record FulfillSellOrderRequest(
    Guid Id,
    IEnumerable<FulfillSellOrderItemRequest> Items);

public record FulfillSellOrderItemRequest(
    Guid ProductId,
    decimal Quantity);