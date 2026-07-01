using ZEN.Application.DTOs.Products;

namespace ZEN.Application.Services.Interfaces;

public interface IBrandService
{
    Task<BrandDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<BrandDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<BrandDto>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<BrandDto> CreateAsync(CreateBrandRequest request, CancellationToken cancellationToken = default);
    Task<BrandDto> UpdateAsync(UpdateBrandRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
