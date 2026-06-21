namespace ZEN.Domain.Entities;

public class VariantAttribute : BaseEntity
{
    public Guid ProductVariantId { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public string AttributeValue { get; set; } = string.Empty;

    public ProductVariant ProductVariant { get; set; } = null!;
}
