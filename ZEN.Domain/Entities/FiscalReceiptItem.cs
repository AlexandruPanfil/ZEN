namespace ZEN.Domain.Entities;

public class FiscalReceiptItem : BaseEntity
{
    public Guid FiscalReceiptId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public string? TaxGroupCode { get; set; }
    public decimal Total { get; set; }

    // Navigation properties
    public FiscalReceipt? FiscalReceipt { get; set; }
}
