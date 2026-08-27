namespace ZEN.Application.DTOs.FiscalCloud;

public record FiscalCloudCloseDayRequestDto(
    string Id,
    bool PrintOnServer,
    string? PrintFormMediaType,
    string? PdfPrintFormSettings,
    string? ImagePrintFormSettings,
    bool Print,
    bool SendToEmail,
    string? EmailAddress,
    bool SendToPhone,
    string? PhoneNumber,
    string AdditionalHeaderText,
    string AdditionalFooterText);