namespace ZEN.Domain.Entities;

public class CashCount : BaseEntity
{
    public Guid ShiftId { get; set; }
    public string DenominationDetails { get; set; } = "{}";
    public decimal TotalCounted { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Shift Shift { get; set; } = null!;
}
