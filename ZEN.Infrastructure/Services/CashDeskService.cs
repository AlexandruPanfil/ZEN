using ZEN.Application.DTOs.Admin;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Enums;
using ZEN.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ZEN.Infrastructure.Services;

public class CashDeskService : ICashDeskService
{
    private readonly ZenDbContext _dbContext;

    public CashDeskService(ZenDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CashDeskDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cashDesk = await _dbContext.CashDesks.FindAsync([id], cancellationToken);
        return cashDesk == null ? null : new CashDeskDto(
            cashDesk.Id,
            cashDesk.Name,
            cashDesk.StoreId,
            cashDesk.IsActive,
            cashDesk.FiscalPrinterId,
            cashDesk.CustomerDisplayId);
    }

    public async Task<CashDeskDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cashDesk = await _dbContext.CashDesks
            .Include(cd => cd.FiscalPrinter)
            .Include(cd => cd.CustomerDisplay)
            .Include(cd => cd.PaymentTerminals)
            .Include(cd => cd.Scanners)
            .FirstOrDefaultAsync(cd => cd.Id == id, cancellationToken);

        if (cashDesk == null)
            return null;

        return new CashDeskDetailDto(
            cashDesk.Id,
            cashDesk.Name,
            cashDesk.StoreId,
            cashDesk.IsActive,
            cashDesk.FiscalPrinterId,
            cashDesk.FiscalPrinter?.Name ?? string.Empty,
            cashDesk.CustomerDisplayId,
            cashDesk.CustomerDisplay?.Name ?? string.Empty,
            cashDesk.PaymentTerminals.Select(pt => new PaymentTerminalDto(
                pt.Id,
                pt.Name,
                pt.TerminalType,
                pt.SerialNumber,
                pt.IpAddress,
                pt.Port,
                pt.CashDeskId,
                pt.IsActive)),
            cashDesk.Scanners.Select(s => new ScannerDto(
                s.Id,
                s.Name,
                s.ScannerType.ToString(),
                s.ConnectionType,
                s.ConnectionString,
                s.CashDeskId,
                s.IsActive)));
    }

    public async Task<IEnumerable<CashDeskDto>> GetAllAsync(Guid? storeId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CashDesks.AsQueryable();

        if (storeId.HasValue)
        {
            query = query.Where(cd => cd.StoreId == storeId.Value);
        }

        var cashDesks = await query.ToListAsync(cancellationToken);
        return cashDesks.Select(cd => new CashDeskDto(
            cd.Id,
            cd.Name,
            cd.StoreId,
            cd.IsActive,
            cd.FiscalPrinterId,
            cd.CustomerDisplayId));
    }

    public async Task<CashDeskDto> CreateAsync(CreateCashDeskRequest request, CancellationToken cancellationToken = default)
    {
        if (request.StoreId.HasValue)
        {
            var storeExists = await _dbContext.Stores.AnyAsync(s => s.Id == request.StoreId.Value && !s.IsDeleted, cancellationToken);
            if (!storeExists)
                throw new InvalidOperationException($"Store with ID {request.StoreId} not found.");
        }

        if (request.FiscalPrinterId.HasValue)
        {
            var fiscalPrinterExists = await _dbContext.Printers.AnyAsync(p => p.Id == request.FiscalPrinterId.Value && !p.IsDeleted && p.Type == PrinterType.Fiscal, cancellationToken);
            if (!fiscalPrinterExists)
                throw new InvalidOperationException($"Fiscal printer with ID {request.FiscalPrinterId} not found.");
        }

        if (request.CustomerDisplayId.HasValue)
        {
            var customerDisplayExists = await _dbContext.CustomerDisplays.AnyAsync(cd => cd.Id == request.CustomerDisplayId.Value && !cd.IsDeleted, cancellationToken);
            if (!customerDisplayExists)
                throw new InvalidOperationException($"Customer display with ID {request.CustomerDisplayId} not found.");
        }

        var cashDesk = new CashDesk
        {
            Name = request.Name,
            StoreId = request.StoreId,
            IsActive = request.IsActive,
            FiscalPrinterId = request.FiscalPrinterId,
            CustomerDisplayId = request.CustomerDisplayId
        };

        _dbContext.CashDesks.Add(cashDesk);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Assign terminals to this cashdesk
        if (request.TerminalIds != null && request.TerminalIds.Any())
        {
            await AssignTerminalsAsync(cashDesk.Id, request.TerminalIds, cancellationToken);
        }

        return new CashDeskDto(
            cashDesk.Id,
            cashDesk.Name,
            cashDesk.StoreId,
            cashDesk.IsActive,
            cashDesk.FiscalPrinterId,
            cashDesk.CustomerDisplayId);
    }

    public async Task<CashDeskDto> UpdateAsync(UpdateCashDeskRequest request, CancellationToken cancellationToken = default)
    {
        var cashDesk = await _dbContext.CashDesks.FindAsync([request.Id], cancellationToken);
        if (cashDesk == null)
        {
            throw new KeyNotFoundException($"Cash desk with ID {request.Id} not found.");
        }

        if (request.FiscalPrinterId.HasValue)
        {
            var fiscalPrinterExists = await _dbContext.Printers.AnyAsync(p => p.Id == request.FiscalPrinterId.Value && !p.IsDeleted && p.Type == PrinterType.Fiscal, cancellationToken);
            if (!fiscalPrinterExists)
                throw new InvalidOperationException($"Fiscal printer with ID {request.FiscalPrinterId} not found.");
        }

        if (request.CustomerDisplayId.HasValue)
        {
            var customerDisplayExists = await _dbContext.CustomerDisplays.AnyAsync(cd => cd.Id == request.CustomerDisplayId.Value && !cd.IsDeleted, cancellationToken);
            if (!customerDisplayExists)
                throw new InvalidOperationException($"Customer display with ID {request.CustomerDisplayId} not found.");
        }

        cashDesk.Name = request.Name;
        cashDesk.StoreId = request.StoreId;
        cashDesk.IsActive = request.IsActive;
        cashDesk.FiscalPrinterId = request.FiscalPrinterId;
        cashDesk.CustomerDisplayId = request.CustomerDisplayId;

        // Update terminal assignments
        var selectedTerminalIds = request.TerminalIds?.ToHashSet() ?? new HashSet<Guid>();
        var existingTerminals = await _dbContext.PaymentTerminals
            .Where(pt => pt.CashDeskId == request.Id)
            .ToListAsync(cancellationToken);

        foreach (var terminal in existingTerminals)
        {
            if (!selectedTerminalIds.Contains(terminal.Id))
            {
                terminal.CashDeskId = null;
            }
        }

        var terminalsToAssign = await _dbContext.PaymentTerminals
            .Where(pt => selectedTerminalIds.Contains(pt.Id))
            .ToListAsync(cancellationToken);

        foreach (var terminal in terminalsToAssign)
        {
            terminal.CashDeskId = request.Id;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CashDeskDto(
            cashDesk.Id,
            cashDesk.Name,
            cashDesk.StoreId,
            cashDesk.IsActive,
            cashDesk.FiscalPrinterId,
            cashDesk.CustomerDisplayId);
    }

    private async Task AssignTerminalsAsync(Guid cashDeskId, IEnumerable<Guid> terminalIds, CancellationToken cancellationToken)
    {
        var terminals = await _dbContext.PaymentTerminals
            .Where(pt => terminalIds.Contains(pt.Id))
            .ToListAsync(cancellationToken);

        foreach (var terminal in terminals)
        {
            terminal.CashDeskId = cashDeskId;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cashDesk = await _dbContext.CashDesks.FindAsync([id], cancellationToken);
        if (cashDesk == null)
        {
            return false;
        }

        // Unassign all terminals from this cashdesk
        var terminals = await _dbContext.PaymentTerminals
            .Where(pt => pt.CashDeskId == id)
            .ToListAsync(cancellationToken);

        foreach (var terminal in terminals)
        {
            terminal.CashDeskId = null;
        }

        _dbContext.CashDesks.Remove(cashDesk);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}