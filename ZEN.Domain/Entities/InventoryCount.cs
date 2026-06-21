using ZEN.Domain.Enums;

namespace ZEN.Domain.Entities;

public class InventoryCount : BaseEntity
{
    public Guid WarehouseId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public InventoryCountStatus Status { get; set; } = InventoryCountStatus.Draft;
    public string? Notes { get; set; }
    public string UserId { get; set; } = string.Empty;

    public Warehouse Warehouse { get; set; } = null!;
    public ICollection<InventoryCountItem> Items { get; set; } = [];
}
