namespace ZEN.Domain.Entities;

public class CustomerDisplay : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ConnectionType { get; set; }
    public string? ConnectionString { get; set; }
    public string? Size { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CashDeskId { get; set; }

    public CashDesk? CashDesk { get; set; }
}