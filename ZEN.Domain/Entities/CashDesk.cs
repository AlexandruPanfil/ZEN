namespace ZEN.Domain.Entities;

public class CashDesk : BaseEntity
{
    public Guid? StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? FiscalPrinterId { get; set; }
    public Guid? CustomerDisplayId { get; set; }

    public Store? Store { get; set; }
    public Printer? FiscalPrinter { get; set; }
    public CustomerDisplay? CustomerDisplay { get; set; }
    public ICollection<PaymentTerminal> PaymentTerminals { get; set; } = [];
    public ICollection<Scanner> Scanners { get; set; } = [];

    public Guid? MenuId { get; set; }
    public Menu? Menu { get; set; }
}