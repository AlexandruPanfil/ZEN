using ZEN.Application.DTOs.Products;

namespace ZEN.Application.Services.Interfaces;

public interface IProductService
{
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetByBarcodeAsync(string barcode, Guid? storeId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductDto>> GetAllAsync(Guid? storeId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductDto>> SearchAsync(string searchTerm, Guid? storeId = null, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateAsync(UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductGroupDto>> GetProductGroupsAsync(CancellationToken cancellationToken = default);
}
