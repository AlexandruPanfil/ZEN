using ZEN.Domain.Enums;

namespace ZEN.Domain.Entities;

public class Printer : BaseEntity
{
    public Guid StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public PrinterType Type { get; set; }
    public string? ConnectionType { get; set; }
    public string? ConnectionString { get; set; }
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public Store Store { get; set; } = null!;
}
