using ZEN.Domain.Enums;

namespace ZEN.Domain.Entities;

public class StockPurchase : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid WarehouseId { get; set; }
    public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;
    public string? SupplierInvoiceNo { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public StockPurchaseStatus Status { get; set; } = StockPurchaseStatus.Draft;
    public string? Notes { get; set; }
    public string UserId { get; set; } = string.Empty;

    public Supplier Supplier { get; set; } = null!;
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public ICollection<StockPurchaseItem> Items { get; set; } = [];
}
