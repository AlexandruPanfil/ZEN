namespace ZEN.Domain.Entities;

public class PaymentTerminal : BaseEntity
{
    public Guid StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TerminalType { get; set; }
    public string? SerialNumber { get; set; }
    public bool IsActive { get; set; } = true;

    public Store Store { get; set; } = null!;
}
