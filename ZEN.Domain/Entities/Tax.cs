namespace ZEN.Domain.Entities;

public class Tax : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = [];
}
