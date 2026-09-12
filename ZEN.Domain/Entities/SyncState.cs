namespace ZEN.Domain.Entities;

public class SyncState : BaseEntity
{
    public DateTime? LastProductsSyncUtc { get; set; }
    public DateTime? LastSuccessfulSyncUtc { get; set; }
    public string? LastError { get; set; }
    public DateTime? LastSalesSyncUtc { get; set; }
    public DateTime? LastShiftsSyncUtc { get; set; }
    public DateTime? LastInventorySyncUtc { get; set; }
    public DateTime? LastEquipmentSyncUtc { get; set; }
    public string? PullCursor { get; set; }
}

