namespace ZEN.Domain.Entities;

public class Product : BaseEntity
{
    public string? Barcode { get; set; }
    public string? SKU { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? TaxId { get; set; }
    public Guid? SupplierId { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal? Weight { get; set; }
    public bool HasVariants { get; set; } = false;
    public bool TrackExpiry { get; set; } = false;
    public bool TrackSerial { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public Category? Category { get; set; }
    public Brand? Brand { get; set; }
    public Tax? Tax { get; set; }
    public Supplier? Supplier { get; set; }
    public ICollection<ProductImage> Images { get; set; } = [];
    public ICollection<ProductVariant> Variants { get; set; } = [];
    public ICollection<InventoryItem> InventoryItems { get; set; } = [];
}
