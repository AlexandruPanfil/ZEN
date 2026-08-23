namespace ZEN.Application.DTOs.FiscalCloud;

public record FiscalCloudItemDto(
    string? GoodId,
    string Name,
    decimal Quantity,
    decimal Price,
    FiscalCloudModifierMode? ModifierMode,
    decimal? ModifierValue,
    string? TaxGroupCode,
    string Comment
);