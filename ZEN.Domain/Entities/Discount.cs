using ZEN.Domain.Enums;

namespace ZEN.Domain.Entities;

public class Discount : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DiscountType Type { get; set; }
    public decimal Value { get; set; }
    public decimal? MinimumAmount { get; set; }
    public int? MaxUsages { get; set; }
    public int UsedCount { get; set; } = 0;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Coupon> Coupons { get; set; } = [];
    public ICollection<Promotion> Promotions { get; set; } = [];
}
