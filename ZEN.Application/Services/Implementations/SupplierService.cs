using ZEN.Application.DTOs.Suppliers;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class SupplierService : ISupplierService
{
    private readonly IRepository<Supplier> _supplierRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SupplierService(IRepository<Supplier> supplierRepository, IUnitOfWork unitOfWork)
    {
        _supplierRepository = supplierRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SupplierDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
        return supplier == null ? null : MapToDto(supplier);
    }

    public async Task<IEnumerable<SupplierDto>> GetAllAsync(Guid? storeId = null, CancellationToken cancellationToken = default)
    {
        var suppliers = await _supplierRepository.GetAllAsync(cancellationToken);
        return suppliers.Select(MapToDto);
    }

    public async Task<IEnumerable<SupplierDto>> SearchAsync(string searchTerm, Guid? storeId = null, CancellationToken cancellationToken = default)
    {
        var suppliers = await _supplierRepository.FindAsync(
            s => s.Name.Contains(searchTerm) ||
                 (s.ContactPerson != null && s.ContactPerson.Contains(searchTerm)) ||
                 (s.Email != null && s.Email.Contains(searchTerm)) ||
                 (s.Phone != null && s.Phone.Contains(searchTerm)),
            cancellationToken);

        return suppliers.Select(MapToDto);
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = new Supplier
        {
            Name = request.Name,
            ContactPerson = request.ContactPerson,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            IsActive = true,
            TotalPurchases = 0,
            TotalPaid = 0,
            Balance = 0
        };

        await _supplierRepository.AddAsync(supplier, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(supplier);
    }

    public async Task<SupplierDto> UpdateAsync(UpdateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(request.Id, cancellationToken);
        if (supplier == null)
            throw new InvalidOperationException($"Supplier with ID {request.Id} not found.");

        supplier.Name = request.Name;
        supplier.ContactPerson = request.ContactPerson;
        supplier.Email = request.Email;
        supplier.Phone = request.Phone;
        supplier.Address = request.Address;
        supplier.IsActive = request.IsActive;

        _supplierRepository.Update(supplier);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(supplier);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
        if (supplier == null)
            return false;

        _supplierRepository.Remove(supplier);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static SupplierDto MapToDto(Supplier supplier)
    {
        return new SupplierDto(
            supplier.Id,
            supplier.Name,
            supplier.ContactPerson,
            supplier.Email,
            supplier.Phone,
            supplier.Address,
            supplier.TotalPurchases,
            supplier.TotalPaid,
            supplier.Balance,
            supplier.IsActive
        );
    }
}
