namespace ZEN.Domain.Entities;

public class SupplierPayment : BaseEntity
{
    public Guid SupplierId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? StockPurchaseId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public string UserId { get; set; } = string.Empty;

    public Supplier Supplier { get; set; } = null!;
    public PurchaseOrder? PurchaseOrder { get; set; }
    public StockPurchase? StockPurchase { get; set; }
}
