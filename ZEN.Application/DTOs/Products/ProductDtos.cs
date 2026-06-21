namespace ZEN.Application.DTOs.Products;

public record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    string? Barcode,
    string? SKU,
    decimal CostPrice,
    decimal SalePrice,
    Guid? CategoryId,
    string? CategoryName,
    Guid? BrandId,
    string? BrandName,
    Guid? TaxId,
    string? TaxName,
    bool IsActive);

public record CreateProductRequest(
    string Name,
    string? Description,
    string? Barcode,
    string? SKU,
    decimal CostPrice,
    decimal SalePrice,
    Guid? CategoryId,
    Guid? BrandId,
    Guid? TaxId,
    Guid? SupplierId,
    bool TrackStock,
    bool IsActive);

public record UpdateProductRequest(
    Guid Id,
    string Name,
    string? Description,
    string? Barcode,
    string? SKU,
    decimal CostPrice,
    decimal SalePrice,
    Guid? CategoryId,
    Guid? BrandId,
    Guid? TaxId,
    bool IsActive);

public record CategoryDto(Guid Id, string Name, Guid? ParentCategoryId, string? ParentCategoryName);

public record BrandDto(Guid Id, string Name);
