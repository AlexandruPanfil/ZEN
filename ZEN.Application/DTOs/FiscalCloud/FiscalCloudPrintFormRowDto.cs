namespace ZEN.Application.DTOs.FiscalCloud;

public record FiscalCloudPrintFormRowDto(
    string Type,
    IEnumerable<string> Cells,
    bool? SmallerFont,
    bool? DoubleWidth,
    bool? DoubleHeight
);