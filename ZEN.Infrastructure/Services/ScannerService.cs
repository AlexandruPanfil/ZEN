using ZEN.Application.DTOs.Admin;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Enums;
using ZEN.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ZEN.Infrastructure.Services;

public class ScannerService : IScannerService
{
    private readonly ZenDbContext _dbContext;

    public ScannerService(ZenDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ScannerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scanner = await _dbContext.Scanners.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return scanner == null ? null : new ScannerDto(
            scanner.Id,
            scanner.Name,
            scanner.ScannerType.ToString(),
            scanner.ConnectionType,
            scanner.ConnectionString,
            scanner.CashDeskId,
            scanner.IsActive,
            scanner.Version,
            scanner.IsDeleted);
    }

    public async Task<IEnumerable<ScannerDto>> GetAllAsync(
        CancellationToken cancellationToken = default,
        bool includeDeleted = false)
    {
        var query = includeDeleted ? _dbContext.Scanners.IgnoreQueryFilters() : _dbContext.Scanners;
        var scanners = await query.ToListAsync(cancellationToken);
        return scanners.Select(s => new ScannerDto(
            s.Id,
            s.Name,
            s.ScannerType.ToString(),
            s.ConnectionType,
            s.ConnectionString,
            s.CashDeskId,
            s.IsActive,
            s.Version,
            s.IsDeleted));
    }

    public IEnumerable<ScannerDto> GetAll()
    {
        var scanners = _dbContext.Scanners.ToList();
        return scanners.Select(s => new ScannerDto(
            s.Id,
            s.Name,
            s.ScannerType.ToString(),
            s.ConnectionType,
            s.ConnectionString,
            s.CashDeskId,
            s.IsActive,
            s.Version,
            s.IsDeleted));
    }

    public async Task<ScannerDto> CreateAsync(CreateScannerRequest request, CancellationToken cancellationToken = default)
    {
        var scanner = new Scanner
        {
            Id = request.Id ?? Guid.NewGuid(),
            Name = request.Name,
            ScannerType = Enum.Parse<ScannerType>(request.ScannerType),
            ConnectionType = request.ConnectionType,
            ConnectionString = request.ConnectionString,
            IsActive = request.IsActive
        };

        _dbContext.Scanners.Add(scanner);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ScannerDto(
            scanner.Id,
            scanner.Name,
            scanner.ScannerType.ToString(),
            scanner.ConnectionType,
            scanner.ConnectionString,
            scanner.CashDeskId,
            scanner.IsActive,
            scanner.Version,
            scanner.IsDeleted);
    }

    public async Task<ScannerDto> UpdateAsync(UpdateScannerRequest request, CancellationToken cancellationToken = default)
    {
        var scanner = await _dbContext.Scanners.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (scanner == null)
        {
            throw new KeyNotFoundException($"Scanner with ID {request.Id} not found.");
        }

        if (scanner.IsDeleted || (request.BaseVersion != 0 && request.BaseVersion != scanner.Version))
        {
            throw new ZEN.Application.Services.Exceptions.SyncConflictException(
                scanner.Id,
                request.BaseVersion,
                scanner.Version);
        }

        scanner.Name = request.Name;
        scanner.ScannerType = Enum.Parse<ScannerType>(request.ScannerType);
        scanner.ConnectionType = request.ConnectionType;
        scanner.ConnectionString = request.ConnectionString;
        scanner.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ScannerDto(
            scanner.Id,
            scanner.Name,
            scanner.ScannerType.ToString(),
            scanner.ConnectionType,
            scanner.ConnectionString,
            scanner.CashDeskId,
            scanner.IsActive,
            scanner.Version,
            scanner.IsDeleted);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default, long baseVersion = 0)
    {
        var scanner = await _dbContext.Scanners.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (scanner == null)
        {
            return false;
        }

        if (scanner.IsDeleted || (baseVersion != 0 && baseVersion != scanner.Version))
        {
            throw new ZEN.Application.Services.Exceptions.SyncConflictException(
                scanner.Id,
                baseVersion,
                scanner.Version);
        }

        _dbContext.Scanners.Remove(scanner);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}