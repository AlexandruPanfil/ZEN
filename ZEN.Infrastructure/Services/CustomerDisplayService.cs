using ZEN.Application.DTOs.Admin;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ZEN.Infrastructure.Services;

public class CustomerDisplayService : ICustomerDisplayService
{
    private readonly ZenDbContext _dbContext;

    public CustomerDisplayService(ZenDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CustomerDisplayDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var display = await _dbContext.CustomerDisplays.FindAsync([id], cancellationToken);
        return display == null ? null : new CustomerDisplayDto(
            display.Id,
            display.Name,
            display.ConnectionType,
            display.ConnectionString,
            display.Size,
            display.CashDeskId,
            display.IsActive);
    }

    public async Task<IEnumerable<CustomerDisplayDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var displays = await _dbContext.CustomerDisplays.ToListAsync(cancellationToken);
        return displays.Select(d => new CustomerDisplayDto(
            d.Id,
            d.Name,
            d.ConnectionType,
            d.ConnectionString,
            d.Size,
            d.CashDeskId,
            d.IsActive));
    }

    public IEnumerable<CustomerDisplayDto> GetAll()
    {
        var displays = _dbContext.CustomerDisplays.ToList();
        return displays.Select(d => new CustomerDisplayDto(
            d.Id,
            d.Name,
            d.ConnectionType,
            d.ConnectionString,
            d.Size,
            d.CashDeskId,
            d.IsActive));
    }

    public async Task<CustomerDisplayDto> CreateAsync(CreateCustomerDisplayRequest request, CancellationToken cancellationToken = default)
    {
        var display = new CustomerDisplay
        {
            Id = request.Id ?? Guid.NewGuid(),
            Name = request.Name,
            ConnectionType = request.ConnectionType,
            ConnectionString = request.ConnectionString,
            Size = request.Size,
            IsActive = request.IsActive
        };

        _dbContext.CustomerDisplays.Add(display);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CustomerDisplayDto(
            display.Id,
            display.Name,
            display.ConnectionType,
            display.ConnectionString,
            display.Size,
            display.CashDeskId,
            display.IsActive);
    }

    public async Task<CustomerDisplayDto> UpdateAsync(UpdateCustomerDisplayRequest request, CancellationToken cancellationToken = default)
    {
        var display = await _dbContext.CustomerDisplays.FindAsync([request.Id], cancellationToken);
        if (display == null)
        {
            throw new KeyNotFoundException($"Customer display with ID {request.Id} not found.");
        }

        display.Name = request.Name;
        display.ConnectionType = request.ConnectionType;
        display.ConnectionString = request.ConnectionString;
        display.Size = request.Size;
        display.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CustomerDisplayDto(
            display.Id,
            display.Name,
            display.ConnectionType,
            display.ConnectionString,
            display.Size,
            display.CashDeskId,
            display.IsActive);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var display = await _dbContext.CustomerDisplays.FindAsync([id], cancellationToken);
        if (display == null)
        {
            return false;
        }

        _dbContext.CustomerDisplays.Remove(display);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}