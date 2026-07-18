using Microsoft.EntityFrameworkCore;
using ZEN.Application.DTOs.Admin;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Infrastructure.Persistence;

namespace ZEN.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ZenDbContext _context;

    public AuditService(ZenDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        string action,
        string entityName,
        string entityId,
        string? oldValues = null,
        string? newValues = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog
        {
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = null,
            UserId = string.Empty,
            UserName = string.Empty
        };

        await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(AuditLogFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.Trim().ToLower();
            query = query.Where(a =>
                a.UserName.ToLower().Contains(searchTerm) ||
                a.EntityName.ToLower().Contains(searchTerm) ||
                a.EntityId.ToLower().Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = query.Where(a => a.Action == filter.Action);
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= filter.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.IpAddress))
        {
            query = query.Where(a => a.IpAddress != null && a.IpAddress.Contains(filter.IpAddress));
        }

        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return logs.Select(a => new AuditLogDto(
            a.Id,
            a.Timestamp,
            a.UserName,
            a.Action,
            a.EntityName,
            a.EntityId,
            a.OldValues,
            a.NewValues,
            a.IpAddress));
    }

    public async Task<int> GetAuditLogsCountAsync(AuditLogFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.Trim().ToLower();
            query = query.Where(a =>
                a.UserName.ToLower().Contains(searchTerm) ||
                a.EntityName.ToLower().Contains(searchTerm) ||
                a.EntityId.ToLower().Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = query.Where(a => a.Action == filter.Action);
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= filter.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.IpAddress))
        {
            query = query.Where(a => a.IpAddress != null && a.IpAddress.Contains(filter.IpAddress));
        }

        return await query.CountAsync(cancellationToken);
    }
}
