namespace ZEN.Domain.Entities;

public class HeldSale : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public Guid StoreId { get; set; }
    public Guid? CustomerId { get; set; }
    public string SerializedItems { get; set; } = "[]";
    public string? Notes { get; set; }
    public DateTime HeldAt { get; set; } = DateTime.UtcNow;

    public Store Store { get; set; } = null!;
    public Customer? Customer { get; set; }
}
