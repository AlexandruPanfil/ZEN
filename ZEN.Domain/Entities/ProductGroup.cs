namespace ZEN.Domain.Entities;

public class ProductGroup : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentGroupId { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public ProductGroup? ParentGroup { get; set; }
    public ICollection<ProductGroup> SubGroups { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
}