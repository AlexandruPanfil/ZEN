namespace ZEN.Domain.Entities;

public class Store : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TaxNumber { get; set; }
    public string? LogoPath { get; set; }
    public string? ReceiptHeader { get; set; }
    public string? ReceiptFooter { get; set; }
    public Guid? CurrencyId { get; set; }
    public bool IsActive { get; set; } = true;

    public Currency? Currency { get; set; }
    public ICollection<Warehouse> Warehouses { get; set; } = [];
    public ICollection<CashDesk> CashDesks { get; set; } = [];
    public ICollection<Shift> Shifts { get; set; } = [];
}