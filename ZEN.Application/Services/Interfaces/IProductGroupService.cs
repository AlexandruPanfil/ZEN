using ZEN.Application.DTOs.Products;

namespace ZEN.Application.Services.Interfaces;

public interface IProductGroupService
{
    Task<ProductGroupDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductGroupDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductGroupDto>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<ProductGroupDto> CreateAsync(CreateProductGroupRequest request, CancellationToken cancellationToken = default);
    Task<ProductGroupDto> UpdateAsync(UpdateProductGroupRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}