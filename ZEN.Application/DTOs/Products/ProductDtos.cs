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

public record CreateCategoryRequest(
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int SortOrder,
    bool IsActive);

public record UpdateCategoryRequest(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int SortOrder,
    bool IsActive);

public record BrandDto(Guid Id, string Name);

public record CreateBrandRequest(
    string Name,
    string? Description,
    bool IsActive);

public record UpdateBrandRequest(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive);
