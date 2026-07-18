using ZEN.Application.DTOs.Admin;

namespace ZEN.Application.Services.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        string action,
        string entityName,
        string entityId,
        string? oldValues = null,
        string? newValues = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);
    Task<int> GetAuditLogsCountAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);
}