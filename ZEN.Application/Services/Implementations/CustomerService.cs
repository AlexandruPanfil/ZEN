using ZEN.Application.DTOs.Customers;
using ZEN.Application.Services.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class CustomerService : ICustomerService
{
    public Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<CustomerDto>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<CustomerDto>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<CustomerDto> UpdateAsync(UpdateCustomerRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<bool> AddLoyaltyPointsAsync(Guid customerId, decimal points, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<bool> RedeemLoyaltyPointsAsync(Guid customerId, decimal points, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
