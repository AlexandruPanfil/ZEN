namespace ZEN.Application.Services.Exceptions;

public sealed class SyncConflictException : Exception
{
    public SyncConflictException(Guid entityId, long expectedVersion, long currentVersion)
        : base($"Entity {entityId} was changed on the server after this client last synchronized it.")
    {
        EntityId = entityId;
        ExpectedVersion = expectedVersion;
        CurrentVersion = currentVersion;
    }

    public Guid EntityId { get; }
    public long ExpectedVersion { get; }
    public long CurrentVersion { get; }
}

