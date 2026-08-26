namespace ZEN.Application.DTOs.FiscalCloud;

public record FiscalCloudAmountModificationDto(
    FiscalCloudModifierMode Mode,
    decimal Value
);