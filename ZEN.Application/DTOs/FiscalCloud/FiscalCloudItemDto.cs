namespace ZEN.Application.DTOs.FiscalCloud;

public record FiscalCloudItemDto(
    Guid? GoodId,
    string Name,
    decimal Quantity,
    decimal Price,
    string? ModifierMode,
    decimal? ModifierValue,
    string? TaxGroupCode,
    string? Comment
);
