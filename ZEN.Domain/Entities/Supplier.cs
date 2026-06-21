namespace ZEN.Domain.Entities;

public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public decimal TotalPurchases { get; set; } = 0;
    public decimal TotalPaid { get; set; } = 0;
    public decimal Balance { get; set; } = 0;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = [];
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = [];
    public ICollection<StockPurchase> StockPurchases { get; set; } = [];
    public ICollection<SupplierPayment> Payments { get; set; } = [];
}
