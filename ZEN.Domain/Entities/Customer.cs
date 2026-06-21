namespace ZEN.Domain.Entities;

public class Customer : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateTime? BirthDate { get; set; }
    public Guid? CustomerGroupId { get; set; }
    public decimal Balance { get; set; } = 0;
    public decimal LoyaltyPoints { get; set; } = 0;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}".Trim();

    public CustomerGroup? CustomerGroup { get; set; }
    public ICollection<MembershipCard> MembershipCards { get; set; } = [];
    public ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = [];
    public ICollection<Sale> Sales { get; set; } = [];
}
