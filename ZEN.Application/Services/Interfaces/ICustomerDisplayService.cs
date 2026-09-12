using ZEN.Application.DTOs.Admin;

namespace ZEN.Application.Services.Interfaces;

public interface ICustomerDisplayService
{
    Task<CustomerDisplayDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerDisplayDto>> GetAllAsync(
        CancellationToken cancellationToken = default,
        bool includeDeleted = false);
    IEnumerable<CustomerDisplayDto> GetAll();
    Task<CustomerDisplayDto> CreateAsync(CreateCustomerDisplayRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDisplayDto> UpdateAsync(UpdateCustomerDisplayRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default, long baseVersion = 0);
}

public record CreateCustomerDisplayRequest(
    string Name,
    string? ConnectionType,
    string? ConnectionString,
    string? Size,
    bool IsActive,
    Guid? Id = null);

public record UpdateCustomerDisplayRequest(
    Guid Id,
    string Name,
    string? ConnectionType,
    string? ConnectionString,
    string? Size,
    bool IsActive,
    long BaseVersion = 0);
