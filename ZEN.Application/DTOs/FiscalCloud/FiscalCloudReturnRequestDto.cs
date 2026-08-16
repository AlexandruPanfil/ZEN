namespace ZEN.Application.DTOs.FiscalCloud;

public record FiscalCloudReturnRequestDto(
    bool Print,
    bool PrintOnServer,
    string? PrintFormMediaType,
    bool? SendToEmail,
    string? EmailAddress,
    bool? SendToPhone,
    string? PhoneNumber,
    string? Comment,
    IEnumerable<FiscalCloudItemDto> Items,
    IEnumerable<FiscalCloudPaymentDto> Payments,
    string? ReceiptMode
);
