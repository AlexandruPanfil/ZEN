namespace ZEN.Domain.Entities;

public class SyncOutboxItem : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int RetryCount { get; set; }
    public DateTime? LastAttemptUtc { get; set; }
    public DateTime? SyncedAtUtc { get; set; }
    public long? BaseVersion { get; set; }
    public long? ConflictVersion { get; set; }
    public string? ConflictRecordJson { get; set; }
    public string? Error { get; set; }
}
