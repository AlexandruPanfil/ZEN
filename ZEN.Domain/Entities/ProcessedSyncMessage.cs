namespace ZEN.Domain.Entities;

public class ProcessedSyncMessage : BaseEntity
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Source { get; set; } = "POS";
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}