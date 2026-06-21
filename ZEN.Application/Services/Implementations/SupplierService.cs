using ZEN.Application.DTOs.Suppliers;
using ZEN.Application.Services.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class SupplierService : ISupplierService
{
    public Task<SupplierDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<SupplierDto>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<SupplierDto>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<SupplierDto> UpdateAsync(UpdateSupplierRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
