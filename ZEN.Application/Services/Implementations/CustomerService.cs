using ZEN.Application.DTOs.Customers;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class CustomerService : ICustomerService
{
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<CustomerGroup> _customerGroupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerService(
        IRepository<Customer> customerRepository,
        IRepository<CustomerGroup> customerGroupRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _customerGroupRepository = customerGroupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
        if (customer == null)
            return null;

        return await MapToDtoAsync(customer, cancellationToken);
    }

    public async Task<IEnumerable<CustomerDto>> GetAllAsync(Guid? storeId = null, CancellationToken cancellationToken = default)
    {
        var customers = await _customerRepository.GetAllAsync(cancellationToken);
        var result = new List<CustomerDto>();

        foreach (var customer in customers)
        {
            result.Add(await MapToDtoAsync(customer, cancellationToken));
        }

        return result;
    }

    public async Task<IEnumerable<CustomerDto>> SearchAsync(string searchTerm, Guid? storeId = null, CancellationToken cancellationToken = default)
    {
        var customers = await _customerRepository.FindAsync(
            c => c.FirstName.Contains(searchTerm) ||
                 c.LastName.Contains(searchTerm) ||
                 (c.Email != null && c.Email.Contains(searchTerm)) ||
                 (c.Phone != null && c.Phone.Contains(searchTerm)),
            cancellationToken);

        var result = new List<CustomerDto>();
        foreach (var customer in customers)
        {
            result.Add(await MapToDtoAsync(customer, cancellationToken));
        }

        return result;
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        // Parse Name into FirstName and LastName
        var nameParts = request.Name.Trim().Split(' ', 2);
        var firstName = nameParts[0];
        var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

        var customer = new Customer
        {
            FirstName = firstName,
            LastName = lastName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            CustomerGroupId = request.CustomerGroupId,
            Balance = 0,
            LoyaltyPoints = 0,
            IsActive = true
        };

        await _customerRepository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToDtoAsync(customer, cancellationToken);
    }

    public async Task<CustomerDto> UpdateAsync(UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(request.Id, cancellationToken);
        if (customer == null)
            throw new InvalidOperationException($"Customer with ID {request.Id} not found.");

        // Parse Name into FirstName and LastName
        var nameParts = request.Name.Trim().Split(' ', 2);
        customer.FirstName = nameParts[0];
        customer.LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

        customer.Email = request.Email;
        customer.Phone = request.Phone;
        customer.Address = request.Address;
        customer.CustomerGroupId = request.CustomerGroupId;
        customer.IsActive = request.IsActive;

        _customerRepository.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToDtoAsync(customer, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
        if (customer == null)
            return false;

        _customerRepository.Remove(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> AddLoyaltyPointsAsync(Guid customerId, decimal points, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null)
            return false;

        customer.LoyaltyPoints += points;
        _customerRepository.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> RedeemLoyaltyPointsAsync(Guid customerId, decimal points, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null)
            return false;

        if (customer.LoyaltyPoints < points)
            throw new InvalidOperationException("Insufficient loyalty points.");

        customer.LoyaltyPoints -= points;
        _customerRepository.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<CustomerDto> MapToDtoAsync(Customer customer, CancellationToken cancellationToken)
    {
        string? customerGroupName = null;
        if (customer.CustomerGroupId.HasValue)
        {
            var group = await _customerGroupRepository.GetByIdAsync(customer.CustomerGroupId.Value, cancellationToken);
            customerGroupName = group?.Name;
        }

        // Calculate total spent from sales (future enhancement - for now return 0)
        decimal totalSpent = 0;

        return new CustomerDto(
            customer.Id,
            customer.FullName,
            customer.Email,
            customer.Phone,
            customer.Address,
            customer.CustomerGroupId,
            customerGroupName,
            customer.LoyaltyPoints,
            totalSpent,
            customer.IsActive
        );
    }
}
