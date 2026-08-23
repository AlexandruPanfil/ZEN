namespace ZEN.Application.DTOs.FiscalCloud;

public enum FiscalCloudReceiptType
{
    FiscalReceipt,
    AlternativeReceipt
}

public record FiscalCloudReceiptDto(
    string? FiscalDeviceId,
    Guid? Id,
    string? UniqueId,
    int? Number,
    string? NumberPresentation,
    string? DateTimePresentation,
    decimal? TotalAmount,
    decimal? TotalPaid,
    decimal? TotalChange
);

public record FiscalCloudSaleResponse(
    string? ReceiptType,
    FiscalCloudReceiptDto? FiscalReceipt,
    FiscalCloudReceiptDto? AlternativeReceipt
);

public record FiscalCloudApiResponse(
    bool Success,
    string? Message,
    string? FullExceptionMessage,
    string? ErrorType,
    FiscalCloudSaleResponse? Data
);

public record FiscalCloudSaleResponseDto(
    bool Success,
    string? ReceiptType,
    FiscalCloudReceiptDto? FiscalReceipt,
    FiscalCloudReceiptDto? AlternativeReceipt,
    string? ErrorMessage,
    string? PrintFormContent
);
