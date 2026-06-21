using ZEN.Domain.Enums;

namespace ZEN.Domain.Entities;

public class Shift : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public Guid StoreId { get; set; }
    public DateTime OpenTime { get; set; } = DateTime.UtcNow;
    public DateTime? CloseTime { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal ClosingCash { get; set; }
    public ShiftStatus Status { get; set; } = ShiftStatus.Open;
    public string? Notes { get; set; }

    public Store Store { get; set; } = null!;
    public ICollection<Sale> Sales { get; set; } = [];
    public ICollection<CashMovement> CashMovements { get; set; } = [];
    public ICollection<CashCount> CashCounts { get; set; } = [];
}
