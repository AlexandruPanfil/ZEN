namespace ZEN.Domain.Entities;

public class SellOrderItem : BaseEntity
{
    public Guid SellOrderId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal FulfilledQuantity { get; set; } = 0;
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }

    public SellOrder SellOrder { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ProductVariant? ProductVariant { get; set; }
}