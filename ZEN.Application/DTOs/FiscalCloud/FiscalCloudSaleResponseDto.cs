namespace ZEN.Application.DTOs.FiscalCloud;

public record FiscalCloudReceiptDto(
    string? ReceiptNumber,
    DateTime? ReceiptDate,
    decimal? TotalAmount,
    string? QrCodeData,
    string? BarcodeData
);

public record FiscalCloudSaleResponseDto(
    bool Success,
    string? ReceiptType,
    FiscalCloudReceiptDto? FiscalReceipt,
    FiscalCloudReceiptDto? AlternativeReceipt,
    string? ErrorMessage,
    string? PrintFormContent
);
