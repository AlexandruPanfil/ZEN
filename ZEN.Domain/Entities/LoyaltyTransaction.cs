using ZEN.Domain.Enums;

namespace ZEN.Domain.Entities;

public class LoyaltyTransaction : BaseEntity
{
    public Guid CustomerId { get; set; }
    public decimal Points { get; set; }
    public LoyaltyTransactionType Type { get; set; }
    public Guid? SaleId { get; set; }
    public string? Notes { get; set; }

    public Customer Customer { get; set; } = null!;
    public Sale? Sale { get; set; }
}
