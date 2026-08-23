namespace ZEN.Application.DTOs.FiscalCloud;

public record FiscalCloudCloseDayRequestDto(
    string? Id,
    bool PrintOnServer,
    string? PrintFormMediaType
);