namespace ZEN.Windows.Models;

public class PosCartItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxRate { get; set; }

    public decimal LineTotal => (Quantity * UnitPrice - Quantity * Discount) * (1 + TaxRate / 100m);
}