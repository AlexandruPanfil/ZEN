using ZEN.Application.DTOs.Customers;

namespace ZEN.Application.Services.Interfaces;

public interface ICustomerService
{
    Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerDto>> GetAllAsync(Guid? storeId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerDto>> SearchAsync(string searchTerm, Guid? storeId = null, CancellationToken cancellationToken = default);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDto> UpdateAsync(UpdateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> AddLoyaltyPointsAsync(Guid customerId, decimal points, CancellationToken cancellationToken = default);
    Task<bool> RedeemLoyaltyPointsAsync(Guid customerId, decimal points, CancellationToken cancellationToken = default);
}
