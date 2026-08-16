namespace ZEN.Application.DTOs.FiscalCloud;

public record FiscalCloudStatusResponseDto(
    bool IsOnline,
    bool IsConnected,
    string? DeviceStatus,
    string? ErrorMessage,
    DateTime? LastCommunication
);
