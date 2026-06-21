namespace ZEN.Domain.Entities;

public class MembershipCard : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiryDate { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public Customer Customer { get; set; } = null!;
}
