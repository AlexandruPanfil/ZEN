namespace ZEN.Windows.Models;

public class PosCartItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxRate { get; set; }

    public decimal LineTotal => Math.Round(Quantity * UnitPrice - Quantity * Discount, 2);
    
    public decimal LineTaxAmount => TaxRate > 0 
        ? Math.Round(LineTotal * TaxRate / (100m + TaxRate), 2) 
        : 0m;
    
    public decimal LineNetAmount => Math.Round(LineTotal - LineTaxAmount, 2);
}