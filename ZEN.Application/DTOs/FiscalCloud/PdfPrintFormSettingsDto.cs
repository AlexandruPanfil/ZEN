namespace ZEN.Application.DTOs.FiscalCloud;

public record PdfPrintFormSettingsDto(
    int ContentWidth,
    int HorizontalMargin,
    int VerticalMargin,
    int FontSize
);