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
    bool IsDefault);

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
    Guid StoreId,
    bool IsDefault,
    bool IsActive);

public record PaymentTerminalDto(
    Guid Id,
    string Name,
    string TerminalType,
    string IpAddress,
    int Port,
    Guid StoreId,
    bool IsActive);
