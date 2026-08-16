namespace ZEN.Application.DTOs.FiscalCloud;

public record FiscalCloudCloseDayRequestDto(
    bool Print,
    bool PrintOnServer,
    string? PrintFormMediaType
);
