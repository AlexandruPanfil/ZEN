namespace ZEN.Application.DTOs.FiscalCloud;

public record FiscalCloudReturnItemDto(
    string? GoodId,
    string Name,
    decimal Quantity,
    decimal Price,
    FiscalCloudModifierMode? ModifierMode,
    decimal? ModifierValue,
    string? TaxGroupCode,
    decimal? TaxRate,
    decimal? TaxAmount,
    decimal? FinalAmount,
    decimal? FinalTaxAmount,
    string? Comment
);