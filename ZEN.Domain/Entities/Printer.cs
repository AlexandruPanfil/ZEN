using ZEN.Domain.Enums;

namespace ZEN.Domain.Entities;

public class Printer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string PrinterName { get; set; } = string.Empty;
    public PrinterType Type { get; set; }
    public string? ConnectionType { get; set; }
    public string? ConnectionString { get; set; }
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;

    // FiscalCloud-specific fields
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? DeviceId { get; set; }
    public string? PointOfSaleId { get; set; }
    public string? FiscalCloudBaseUrl { get; set; }
}