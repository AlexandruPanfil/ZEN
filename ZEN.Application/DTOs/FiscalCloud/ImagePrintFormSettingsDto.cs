namespace ZEN.Application.DTOs.FiscalCloud;

public record ImagePrintFormSettingsDto(
    int ContentWidth,
    int HorizontalMargin,
    int VerticalMargin,
    int FontSize,
    int Width
);