using ZEN.Domain.Enums;

namespace ZEN.Domain.Entities;

public class StockTransfer : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public Guid SourceWarehouseId { get; set; }
    public Guid DestinationWarehouseId { get; set; }
    public StockTransferStatus Status { get; set; } = StockTransferStatus.Draft;
    public DateTime TransferDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public string UserId { get; set; } = string.Empty;

    public Warehouse SourceWarehouse { get; set; } = null!;
    public Warehouse DestinationWarehouse { get; set; } = null!;
    public ICollection<StockTransferItem> Items { get; set; } = [];
}
