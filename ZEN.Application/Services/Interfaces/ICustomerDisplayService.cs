using ZEN.Application.DTOs.Admin;

namespace ZEN.Application.Services.Interfaces;

public interface ICustomerDisplayService
{
    Task<CustomerDisplayDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerDisplayDto>> GetAllAsync(CancellationToken cancellationToken = default);
    IEnumerable<CustomerDisplayDto> GetAll();
    Task<CustomerDisplayDto> CreateAsync(CreateCustomerDisplayRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDisplayDto> UpdateAsync(UpdateCustomerDisplayRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public record CreateCustomerDisplayRequest(
    string Name,
    string? ConnectionType,
    string? ConnectionString,
    string? Size,
    bool IsActive);

public record UpdateCustomerDisplayRequest(
    Guid Id,
    string Name,
    string? ConnectionType,
    string? ConnectionString,
    string? Size,
    bool IsActive);