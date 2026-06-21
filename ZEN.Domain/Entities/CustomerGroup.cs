namespace ZEN.Domain.Entities;

public class CustomerGroup : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; } = 0;
    public decimal LoyaltyMultiplier { get; set; } = 1;

    public ICollection<Customer> Customers { get; set; } = [];
}
