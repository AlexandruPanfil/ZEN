using ZEN.Application.DTOs.Suppliers;

namespace ZEN.Application.Services.Interfaces;

public interface ISupplierService
{
    Task<SupplierDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SupplierDto>> GetAllAsync(Guid? storeId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<SupplierDto>> SearchAsync(string searchTerm, Guid? storeId = null, CancellationToken cancellationToken = default);
    Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<SupplierDto> UpdateAsync(UpdateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
