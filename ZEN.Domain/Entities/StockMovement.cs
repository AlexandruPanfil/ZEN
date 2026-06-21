using ZEN.Domain.Enums;

namespace ZEN.Domain.Entities;

public class StockMovement : BaseEntity
{
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public StockMovementType MovementType { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public string UserId { get; set; } = string.Empty;

    public Product Product { get; set; } = null!;
    public ProductVariant? ProductVariant { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
}
