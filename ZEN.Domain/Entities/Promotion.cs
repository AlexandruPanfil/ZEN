using ZEN.Domain.Enums;

namespace ZEN.Domain.Entities;

public class Promotion : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid DiscountId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public PromotionAppliesTo AppliesTo { get; set; } = PromotionAppliesTo.All;
    public Guid? ReferenceId { get; set; }
    public bool IsActive { get; set; } = true;

    public Discount Discount { get; set; } = null!;
}
