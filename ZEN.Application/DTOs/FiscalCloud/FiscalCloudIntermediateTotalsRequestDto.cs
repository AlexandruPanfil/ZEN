namespace ZEN.Application.DTOs.FiscalCloud;

public record FiscalCloudIntermediateTotalsRequestDto(
    bool Print,
    bool PrintOnServer,
    string? PrintFormMediaType
);
