using ZEN.Domain.Enums;

namespace ZEN.Application.DTOs.Sync;

public record ProductSyncItemDto(
    Guid Id,
    string Name,
    string? Barcode,
    string? SKU,
    decimal CostPrice,
    decimal SalePrice,
    bool IsActive,
    bool IsDeleted,
    DateTime UpdatedAt);

public record ProductsDeltaResponse(
    DateTime ServerTimeUtc,
    bool HasMore,
    IReadOnlyList<ProductSyncItemDto> Items);

public record PaymentSyncItemDto(
    Guid PaymentId,
    Guid SaleId,
    decimal Amount,
    PaymentMethod Method,
    string? Reference,
    DateTime PaidAt,
    DateTime ProcessedAt,
    string IdempotencyKey);

public record UploadPaymentsRequest(
    string PosDeviceId,
    IReadOnlyList<PaymentSyncItemDto> Items);

public record PaymentSyncResultDto(
    Guid PaymentId,
    string Status,
    string? Message);

public record UploadPaymentsResponse(
    IReadOnlyList<PaymentSyncResultDto> Results);