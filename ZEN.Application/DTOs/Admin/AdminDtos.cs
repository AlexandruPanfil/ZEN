using ZEN.Domain.Enums;

namespace ZEN.Application.DTOs.Admin;

public record StoreDto(
    Guid Id,
    string Name,
    string Code,
    string? Address,
    string? City,
    string? Phone,
    string? Email,
    bool IsActive);

public record CreateStoreRequest(
    string Name,
    string Code,
    string? Address,
    string? City,
    string? Phone,
    string? Email);

public record TaxDto(
    Guid Id,
    string Name,
    decimal Rate,
    bool IsDefault,
    bool IsActive);

public record CurrencyDto(
    Guid Id,
    string Code,
    string Name,
    string Symbol,
    bool IsDefault);

public record WarehouseDto(
    Guid Id,
    string Name,
    string Code,
    Guid StoreId,
    string StoreName,
    bool IsActive);

public record PrinterDto(
    Guid Id,
    string Name,
    string PrinterName,
    string PrinterType,
    string? ConnectionType,
    string? ConnectionString,
    bool IsDefault,
    bool IsActive);

public record PaymentTerminalDto(
    Guid Id,
    string Name,
    string TerminalType,
    string? SerialNumber,
    string? IpAddress,
    int Port,
    Guid? CashDeskId,
    bool IsActive);

public record ScannerDto(
    Guid Id,
    string Name,
    string ScannerType,
    string? ConnectionType,
    string? ConnectionString,
    Guid? CashDeskId,
    bool IsActive);

public record CustomerDisplayDto(
    Guid Id,
    string Name,
    string? ConnectionType,
    string? ConnectionString,
    string? Size,
    Guid? CashDeskId,
    bool IsActive);

public record CashDeskDto(
    Guid Id,
    string Name,
    Guid? StoreId,
    bool IsActive,
    Guid? FiscalPrinterId,
    Guid? CustomerDisplayId);

public record CashDeskDetailDto(
    Guid Id,
    string Name,
    Guid? StoreId,
    bool IsActive,
    Guid? FiscalPrinterId,
    string FiscalPrinterName,
    Guid? CustomerDisplayId,
    string CustomerDisplayName,
    IEnumerable<PaymentTerminalDto> PaymentTerminals,
    IEnumerable<ScannerDto> Scanners);