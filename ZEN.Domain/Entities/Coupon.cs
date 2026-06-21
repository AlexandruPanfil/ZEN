namespace ZEN.Domain.Entities;

public class Coupon : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid DiscountId { get; set; }
    public bool IsUsed { get; set; } = false;
    public Guid? UsedBySaleId { get; set; }
    public DateTime? UsedAt { get; set; }

    public Discount Discount { get; set; } = null!;
    public Sale? UsedBySale { get; set; }
}
