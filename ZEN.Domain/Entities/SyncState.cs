namespace ZEN.Domain.Entities;

public class SyncState : BaseEntity
{
    public DateTime? LastProductsSyncUtc { get; set; }
    public DateTime? LastSuccessfulSyncUtc { get; set; }
    public string? LastError { get; set; }
}