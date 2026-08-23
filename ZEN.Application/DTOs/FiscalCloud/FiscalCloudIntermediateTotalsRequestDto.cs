namespace ZEN.Application.DTOs.FiscalCloud;

public record FiscalCloudIntermediateTotalsRequestDto(
    string? Id,
    bool PrintOnServer,
    string? PrintFormMediaType
);