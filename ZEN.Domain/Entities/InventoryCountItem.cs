namespace ZEN.Domain.Entities;

public class InventoryCountItem : BaseEntity
{
    public Guid InventoryCountId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal Discrepancy => CountedQuantity - ExpectedQuantity;

    public InventoryCount InventoryCount { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ProductVariant? ProductVariant { get; set; }
}
