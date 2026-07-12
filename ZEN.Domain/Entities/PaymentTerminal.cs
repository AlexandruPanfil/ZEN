namespace ZEN.Domain.Entities;

public class PaymentTerminal : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string TerminalType { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? IpAddress { get; set; }
    public int Port { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public Guid? CashDeskId { get; set; }

    public CashDesk? CashDesk { get; set; }
}