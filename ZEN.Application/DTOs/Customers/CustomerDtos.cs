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

public class UpdateCustomerRequest
{
    public UpdateCustomerRequest() { }

    public UpdateCustomerRequest(Guid id, string name, string? email, string? phone, string? address, Guid? customerGroupId, bool isActive)
    {
        Id = id;
        Name = name;
        Email = email;
        Phone = phone;
        Address = address;
        CustomerGroupId = customerGroupId;
        IsActive = isActive;
    }

    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public Guid? CustomerGroupId { get; set; }
    public bool IsActive { get; set; }
}

public record CustomerGroupDto(
    Guid Id,
    string Name,
    decimal DiscountPercentage);
