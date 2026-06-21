using ZEN.Domain.Interfaces;

namespace ZEN.Infrastructure.Services;

public class AuditService : IAuditService
{
    // TODO: Implement audit logging logic when needed
    // For now, this is a placeholder implementation

    public Task LogAsync(
        string action,
        string entityName,
        string entityId,
        string? oldValues = null,
        string? newValues = null,
        CancellationToken cancellationToken = default)
    {
        // Future: Write to AuditLog table
        return Task.CompletedTask;
    }
}
