namespace ZEN.Application.DTOs.Suppliers;

public record SupplierDto(
    Guid Id,
    string Name,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Address,
    decimal TotalPurchases,
    decimal TotalPaid,
    decimal Balance,
    bool IsActive);

public record CreateSupplierRequest(
    string Name,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Address);

public record UpdateSupplierRequest(
    Guid Id,
    string Name,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Address,
    bool IsActive);
