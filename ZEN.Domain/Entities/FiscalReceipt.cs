using ZEN.Domain.Enums;

namespace ZEN.Domain.Entities;

public class FiscalReceipt : BaseEntity
{
    public Guid SaleId { get; set; }
    public string? FiscalReceiptNumber { get; set; }
    public DateTime? FiscalReceiptDate { get; set; }
    public FiscalOperationType OperationType { get; set; }
    public string? RequestJson { get; set; }
    public string? ResponseJson { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation properties
    public Sale? Sale { get; set; }
    public ICollection<FiscalReceiptItem> Items { get; set; } = [];
}
