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
    Guid? ProductGroupId,
    string? ProductGroupName,
    Guid? TaxId,
    string? TaxName,
    decimal TaxRate,
    bool IsActive);

public record CreateProductRequest(
    string Name,
    string? Description,
    string? Barcode,
    string? SKU,
    decimal CostPrice,
    decimal SalePrice,
    Guid? CategoryId,
    Guid? ProductGroupId,
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
    Guid? ProductGroupId,
    Guid? TaxId,
    bool IsActive);

public record CategoryDto(Guid Id, string Name, string? Description, Guid? ParentCategoryId, string? ParentCategoryName, int SortOrder, bool IsActive);

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

public record ProductGroupDto(Guid Id, string Name, string? Description, Guid? ParentGroupId, string? ParentGroupName, int SortOrder, bool IsActive);

public record CreateProductGroupRequest(
    string Name,
    string? Description,
    Guid? ParentGroupId,
    int SortOrder,
    bool IsActive);

public record UpdateProductGroupRequest(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentGroupId,
    int SortOrder,
    bool IsActive);
