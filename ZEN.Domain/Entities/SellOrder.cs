using ZEN.Domain.Enums;

namespace ZEN.Domain.Entities;

public class SellOrder : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid StoreId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDate { get; set; }
    public SellOrderStatus Status { get; set; } = SellOrderStatus.Draft;
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public string UserId { get; set; } = string.Empty;

    public Customer Customer { get; set; } = null!;
    public Store Store { get; set; } = null!;
    public ICollection<SellOrderItem> Items { get; set; } = [];
}