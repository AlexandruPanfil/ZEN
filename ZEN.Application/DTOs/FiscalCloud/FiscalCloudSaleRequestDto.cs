using System.Text.Json.Serialization;

namespace ZEN.Application.DTOs.FiscalCloud;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FiscalCloudReceiptMode
{
    FiscalReceiptOnly,
    AlternativeReceiptOnly,
    FiscalThenAlternativeReceipt
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FiscalCloudModifierMode
{
    ByPercent,
    ByAmount
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FiscalCloudPrintFormMediaType
{
    Json,
    Html,
    Pdf,
    Image
}

public record FiscalCloudSaleRequestDto(
    string Id,
    bool PrintOnServer,
    FiscalCloudPrintFormMediaType PrintFormMediaType,
    PdfPrintFormSettingsDto PdfPrintFormSettings,
    ImagePrintFormSettingsDto ImagePrintFormSettings,
    bool Print,
    bool SendToEmail,
    string EmailAddress,
    bool SendToPhone,
    string PhoneNumber,
    string AdditionalHeaderText,
    string AdditionalFooterText,
    IEnumerable<FiscalCloudItemDto> Items,
    IEnumerable<FiscalCloudPaymentDto> Payments,
    IEnumerable<FiscalCloudAmountModificationDto> AmountModifications,
    FiscalCloudReceiptMode ReceiptMode,
    string AlternativeReceiptSeriesAndNumber
);