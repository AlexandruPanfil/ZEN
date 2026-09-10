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
        var scanner = await _dbContext.Scanners.FindAsync([id], cancellationToken);
        return scanner == null ? null : new ScannerDto(
            scanner.Id,
            scanner.Name,
            scanner.ScannerType.ToString(),
            scanner.ConnectionType,
            scanner.ConnectionString,
            scanner.CashDeskId,
            scanner.IsActive);
    }

    public async Task<IEnumerable<ScannerDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var scanners = await _dbContext.Scanners.ToListAsync(cancellationToken);
        return scanners.Select(s => new ScannerDto(
            s.Id,
            s.Name,
            s.ScannerType.ToString(),
            s.ConnectionType,
            s.ConnectionString,
            s.CashDeskId,
            s.IsActive));
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
            s.IsActive));
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
            scanner.IsActive);
    }

    public async Task<ScannerDto> UpdateAsync(UpdateScannerRequest request, CancellationToken cancellationToken = default)
    {
        var scanner = await _dbContext.Scanners.FindAsync([request.Id], cancellationToken);
        if (scanner == null)
        {
            throw new KeyNotFoundException($"Scanner with ID {request.Id} not found.");
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
            scanner.IsActive);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scanner = await _dbContext.Scanners.FindAsync([id], cancellationToken);
        if (scanner == null)
        {
            return false;
        }

        _dbContext.Scanners.Remove(scanner);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}