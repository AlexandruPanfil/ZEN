namespace ZEN.Application.DTOs.Customers;

public record CustomerDto(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    Guid? CustomerGroupId,
    string? CustomerGroupName,
    decimal LoyaltyPoints,
    decimal TotalSpent,
    bool IsActive);

public record CreateCustomerRequest(
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    Guid? CustomerGroupId);

public record UpdateCustomerRequest(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    Guid? CustomerGroupId,
    bool IsActive);

public record CustomerGroupDto(
    Guid Id,
    string Name,
    decimal DiscountPercentage);
