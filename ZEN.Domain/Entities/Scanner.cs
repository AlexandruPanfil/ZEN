using ZEN.Domain.Enums;

namespace ZEN.Domain.Entities;

public class Scanner : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ScannerType ScannerType { get; set; } = ScannerType.Barcode;
    public string? ConnectionType { get; set; }
    public string? ConnectionString { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CashDeskId { get; set; }

    public CashDesk? CashDesk { get; set; }
}