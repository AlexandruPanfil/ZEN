namespace ZEN.Domain.Entities;

public class Warehouse : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public Guid StoreId { get; set; }
    public bool IsMain { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public Store Store { get; set; } = null!;
    public ICollection<InventoryItem> InventoryItems { get; set; } = [];
    public ICollection<StockMovement> StockMovements { get; set; } = [];
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = [];
    public ICollection<InventoryCount> InventoryCounts { get; set; } = [];
}
