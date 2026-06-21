namespace ZEN.Domain.Entities;

public class ProductImage : BaseEntity
{
    public Guid ProductId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public bool IsMain { get; set; } = false;
    public int SortOrder { get; set; } = 0;

    public Product Product { get; set; } = null!;
}
