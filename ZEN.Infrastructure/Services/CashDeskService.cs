using ZEN.Application.DTOs.Admin;
using ZEN.Application.DTOs.CashDeskMenu;
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
        return cashDesk == null ? null : ToDto(cashDesk);
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
            cashDesk.MenuId,
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
                s.IsActive)),
            cashDesk.LastActivityUtc,
            cashDesk.IsOnline);
    }

    public async Task<IEnumerable<CashDeskDto>> GetAllAsync(Guid? storeId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CashDesks.AsQueryable();

        if (storeId.HasValue)
        {
            query = query.Where(cd => cd.StoreId == storeId.Value);
        }

        var cashDesks = await query.ToListAsync(cancellationToken);
        return cashDesks.Select(ToDto);
    }

    public async Task<CashDeskDto> CreateAsync(CreateCashDeskRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateReferences(request.StoreId, request.FiscalPrinterId, request.CustomerDisplayId, request.MenuId, cancellationToken);

        var cashDesk = new CashDesk
        {
            Id = request.Id ?? Guid.NewGuid(),
            Name = request.Name,
            StoreId = request.StoreId,
            IsActive = request.IsActive,
            FiscalPrinterId = request.FiscalPrinterId,
            CustomerDisplayId = request.CustomerDisplayId,
            MenuId = request.MenuId
        };

        _dbContext.CashDesks.Add(cashDesk);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (request.TerminalIds != null && request.TerminalIds.Any())
        {
            await AssignTerminalsAsync(cashDesk.Id, request.TerminalIds, cancellationToken);
        }

        return ToDto(cashDesk);
    }

    public async Task<CashDeskDto> UpdateAsync(UpdateCashDeskRequest request, CancellationToken cancellationToken = default)
    {
        var cashDesk = await _dbContext.CashDesks.FindAsync([request.Id], cancellationToken);
        if (cashDesk == null)
        {
            throw new KeyNotFoundException($"Cash desk with ID {request.Id} not found.");
        }

        await ValidateReferences(request.StoreId, request.FiscalPrinterId, request.CustomerDisplayId, request.MenuId, cancellationToken);

        cashDesk.Name = request.Name;
        cashDesk.StoreId = request.StoreId;
        cashDesk.IsActive = request.IsActive;
        cashDesk.FiscalPrinterId = request.FiscalPrinterId;
        cashDesk.CustomerDisplayId = request.CustomerDisplayId;
        cashDesk.MenuId = request.MenuId;

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

        return ToDto(cashDesk);
    }

    private async Task ValidateReferences(Guid? storeId, Guid? fiscalPrinterId, Guid? customerDisplayId, Guid? menuId, CancellationToken cancellationToken)
    {
        if (storeId.HasValue)
        {
            var storeExists = await _dbContext.Stores.AnyAsync(s => s.Id == storeId.Value && !s.IsDeleted, cancellationToken);
            if (!storeExists)
                throw new InvalidOperationException($"Store with ID {storeId} not found.");
        }

        if (fiscalPrinterId.HasValue)
        {
            var fiscalPrinterExists = await _dbContext.Printers.AnyAsync(p => p.Id == fiscalPrinterId.Value && !p.IsDeleted && p.Type == PrinterType.Fiscal, cancellationToken);
            if (!fiscalPrinterExists)
                throw new InvalidOperationException($"Fiscal printer with ID {fiscalPrinterId} not found.");
        }

        if (customerDisplayId.HasValue)
        {
            var customerDisplayExists = await _dbContext.CustomerDisplays.AnyAsync(cd => cd.Id == customerDisplayId.Value && !cd.IsDeleted, cancellationToken);
            if (!customerDisplayExists)
                throw new InvalidOperationException($"Customer display with ID {customerDisplayId} not found.");
        }

        if (menuId.HasValue)
        {
            var menuExists = await _dbContext.Menus.AnyAsync(m => m.Id == menuId.Value && !m.IsDeleted, cancellationToken);
            if (!menuExists)
                throw new InvalidOperationException($"Menu with ID {menuId} not found.");
        }
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

    public async Task<CashDeskDto?> HeartbeatAsync(Guid cashDeskId, CancellationToken cancellationToken = default)
    {
        var cashDesk = await _dbContext.CashDesks.FindAsync([cashDeskId], cancellationToken);
        if (cashDesk == null)
        {
            return null;
        }

        cashDesk.LastActivityUtc = DateTime.UtcNow;
        cashDesk.IsOnline = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(cashDesk);
    }

    public async Task<IEnumerable<CashDeskDto>> GetAllWithStatusAsync(Guid? storeId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CashDesks.AsQueryable();

        if (storeId.HasValue)
        {
            query = query.Where(cd => cd.StoreId == storeId.Value);
        }

        // Consider a cash desk offline if no heartbeat for 2 minutes
        var offlineThreshold = DateTime.UtcNow.AddMinutes(-2);
        var cashDesks = await query.ToListAsync(cancellationToken);
        
        // Update IsOnline status based on last activity
        foreach (var cd in cashDesks)
        {
            cd.IsOnline = cd.LastActivityUtc.HasValue && cd.LastActivityUtc.Value > offlineThreshold;
        }
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        return cashDesks.Select(ToDto);
    }

    public async Task<CashDeskDto> AssignMenuAsync(AssignMenuToCashDeskRequest request, CancellationToken cancellationToken = default)
    {
        var cashDesk = await _dbContext.CashDesks.FindAsync([request.CashDeskId], cancellationToken);
        if (cashDesk == null)
            throw new KeyNotFoundException($"Cash desk with ID {request.CashDeskId} not found.");

        if (request.MenuId.HasValue)
        {
            var menuExists = await _dbContext.Menus.AnyAsync(m => m.Id == request.MenuId.Value && !m.IsDeleted, cancellationToken);
            if (!menuExists)
                throw new InvalidOperationException($"Menu with ID {request.MenuId} not found.");
        }

        cashDesk.MenuId = request.MenuId;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(cashDesk);
    }

    private static CashDeskDto ToDto(CashDesk cashDesk) => new(
        cashDesk.Id,
        cashDesk.Name,
        cashDesk.StoreId,
        cashDesk.IsActive,
        cashDesk.FiscalPrinterId,
        cashDesk.CustomerDisplayId,
        cashDesk.MenuId,
        cashDesk.LastActivityUtc,
        cashDesk.IsOnline);
}
