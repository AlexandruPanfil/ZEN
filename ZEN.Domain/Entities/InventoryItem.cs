namespace ZEN.Domain.Entities;

public class InventoryItem : BaseEntity
{
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal Quantity { get; set; } = 0;
    public decimal MinQuantity { get; set; } = 0;
    public decimal MaxQuantity { get; set; } = 0;
    public DateTime? ExpiryDate { get; set; }
    public string? BatchNumber { get; set; }
    public string? SerialNumber { get; set; }

    public Product Product { get; set; } = null!;
    public ProductVariant? ProductVariant { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
}
