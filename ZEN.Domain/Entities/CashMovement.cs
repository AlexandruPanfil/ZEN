using ZEN.Domain.Enums;

namespace ZEN.Domain.Entities;

public class CashMovement : BaseEntity
{
    public Guid ShiftId { get; set; }
    public decimal Amount { get; set; }
    public CashMovementType Type { get; set; }
    public string? Notes { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Shift Shift { get; set; } = null!;
}
