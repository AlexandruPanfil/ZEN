using ZEN.Domain.Enums;

namespace ZEN.Domain.Entities;

public class Sale : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public Guid? CustomerId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid? ShiftId { get; set; }
    public Guid StoreId { get; set; }
    public Guid? CashDeskId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal LoyaltyPointsEarned { get; set; }
    public decimal LoyaltyPointsRedeemed { get; set; }
    public SaleStatus Status { get; set; } = SaleStatus.Completed;
    public string? Notes { get; set; }

    public Customer? Customer { get; set; }
    public Shift? Shift { get; set; }
    public Store Store { get; set; } = null!;
    public ICollection<SaleItem> Items { get; set; } = [];
    public ICollection<SalePayment> Payments { get; set; } = [];
    public ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = [];
    public ICollection<FiscalReceipt> FiscalReceipts { get; set; } = [];
}
