using ZEN.Domain.Enums;

namespace ZEN.Application.DTOs.Admin;

public record AuditLogDto(
    Guid Id,
    DateTime Timestamp,
    string UserName,
    string Action,
    string EntityName,
    string EntityId,
    string? OldValues,
    string? NewValues,
    string? IpAddress);

public record AuditLogFilter(
    string? SearchTerm = null,
    string? Action = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? IpAddress = null,
    int PageNumber = 1,
    int PageSize = 50);

    public record StoreDto(
    Guid Id,
    string Name,
    string Code,
    string? Address,
    string? City,
    string? Phone,
    string? Email,
    bool IsActive,
    long Version = 0,
    bool IsDeleted = false,
    string? CurrencyCode = null);

    public record CreateStoreRequest(
    string Name,
    string Code,
    string? Address,
    string? City,
    string? Phone,
    string? Email,
    string? CurrencyCode = null,
    Guid? Id = null);

    public record UpdateStoreRequest(
    Guid Id,
    string Name,
    string Code,
    string? Address,
    string? City,
    string? Phone,
    string? Email,
    bool IsActive,
    long BaseVersion = 0,
    string? CurrencyCode = null);

    public record StoreEditResult(
        string Code,
        string Name,
        string? Address,
        string? City,
        string? Phone,
        string? Email,
        string CurrencyCode,
        bool IsActive);

public record TaxDto(Guid Id, string Name, decimal Rate, bool IsDefault, bool IsActive);

public record CurrencyDto(Guid Id, string Code, string Name, string Symbol, bool IsDefault);

public record WarehouseDto(Guid Id, string Name, string Code, Guid StoreId, string StoreName, bool IsActive);

public record PrinterDto(
    Guid Id,
    string Name,
    string PrinterType,
    string? FiscalDeviceType,
    bool IsDefault,
    bool IsActive,
    string? ApiKey,
    string? ApiSecret,
    string? DeviceId,
     string? PointOfSaleId,
     string? FiscalCloudBaseUrl,
     int? FiscalCloudTimeoutSeconds = null,
     long Version = 0,
     bool IsDeleted = false);

public record PaymentTerminalDto(
    Guid Id,
    string Name,
    string TerminalType,
    string? SerialNumber,
    string? IpAddress,
    int Port,
    Guid? CashDeskId,
    bool IsActive,
    long Version = 0,
    bool IsDeleted = false);

public record ScannerDto(
    Guid Id,
    string Name,
    string ScannerType,
    string? ConnectionType,
    string? ConnectionString,
    Guid? CashDeskId,
    bool IsActive,
    long Version = 0,
    bool IsDeleted = false);

public record CustomerDisplayDto(
    Guid Id,
    string Name,
    string? ConnectionType,
    string? ConnectionString,
    string? Size,
    Guid? CashDeskId,
    bool IsActive,
    long Version = 0,
    bool IsDeleted = false);

public record CashDeskDto(
    Guid Id,
    string Name,
    Guid? StoreId,
    bool IsActive,
    Guid? FiscalPrinterId,
    Guid? CustomerDisplayId,
    Guid? MenuId,
    DateTime? LastActivityUtc,
    bool IsOnline,
    long Version = 0,
    bool IsDeleted = false);

public record CashDeskDetailDto(
    Guid Id,
    string Name,
    Guid? StoreId,
    bool IsActive,
    Guid? FiscalPrinterId,
    string FiscalPrinterName,
    Guid? CustomerDisplayId,
    string CustomerDisplayName,
    Guid? MenuId,
    IEnumerable<PaymentTerminalDto> PaymentTerminals,
    IEnumerable<ScannerDto> Scanners,
    DateTime? LastActivityUtc,
    bool IsOnline,
    long Version = 0,
    bool IsDeleted = false);
