namespace ZEN.Application.DTOs.FiscalCloud;

public record FiscalCloudReturnRequestDto(
    string? Id,
    bool Print,
    bool PrintOnServer,
    string? PrintFormMediaType,
    string? OriginalReceiptId,
    bool? IssueCashOperationWhenNecessary,
    IEnumerable<FiscalCloudReturnItemDto> Items,
    IEnumerable<FiscalCloudReturnPaymentDto> Payments
);