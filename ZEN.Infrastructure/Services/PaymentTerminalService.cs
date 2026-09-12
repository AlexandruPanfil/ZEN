using ZEN.Application.DTOs.Admin;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ZEN.Infrastructure.Services
{
    public class PaymentTerminalService : IPaymentTerminalService
    {
        private readonly ZenDbContext _dbContext;

        public PaymentTerminalService(ZenDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PaymentTerminalDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var terminal = await _dbContext.PaymentTerminals.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            return terminal == null ? null : new PaymentTerminalDto(
                terminal.Id,
                terminal.Name,
                terminal.TerminalType,
                terminal.SerialNumber,
                terminal.IpAddress,
                terminal.Port,
                terminal.CashDeskId,
                terminal.IsActive,
                terminal.Version,
                terminal.IsDeleted);
        }

        public async Task<IEnumerable<PaymentTerminalDto>> GetAllAsync(
            CancellationToken cancellationToken = default,
            bool includeDeleted = false)
        {
            var query = includeDeleted ? _dbContext.PaymentTerminals.IgnoreQueryFilters() : _dbContext.PaymentTerminals;
            var terminals = await query.ToListAsync(cancellationToken);
            return terminals.Select(t => new PaymentTerminalDto(
                t.Id,
                t.Name,
                t.TerminalType,
                t.SerialNumber,
                t.IpAddress,
                t.Port,
                t.CashDeskId,
                t.IsActive,
                t.Version,
                t.IsDeleted));
        }

        public IEnumerable<PaymentTerminalDto> GetAll()
        {
            var terminals = _dbContext.PaymentTerminals.ToList();
            return terminals.Select(t => new PaymentTerminalDto(
                t.Id,
                t.Name,
                t.TerminalType,
                t.SerialNumber,
                t.IpAddress,
                t.Port,
                t.CashDeskId,
                t.IsActive,
                t.Version,
                t.IsDeleted));
        }

        public async Task<PaymentTerminalDto> CreateAsync(CreatePaymentTerminalRequest request, CancellationToken cancellationToken = default)
        {
            var terminal = new PaymentTerminal
            {
                Id = request.Id ?? Guid.NewGuid(),
                Name = request.Name,
                TerminalType = request.TerminalType,
                SerialNumber = request.SerialNumber,
                IpAddress = request.IpAddress,
                Port = request.Port,
                IsActive = request.IsActive
            };

            _dbContext.PaymentTerminals.Add(terminal);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new PaymentTerminalDto(
                terminal.Id,
                terminal.Name,
                terminal.TerminalType,
                terminal.SerialNumber,
                terminal.IpAddress,
                terminal.Port,
                terminal.CashDeskId,
                terminal.IsActive,
                terminal.Version,
                terminal.IsDeleted);
        }

        public async Task<PaymentTerminalDto> UpdateAsync(UpdatePaymentTerminalRequest request, CancellationToken cancellationToken = default)
        {
            var terminal = await _dbContext.PaymentTerminals.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            if (terminal == null)
            {
                throw new KeyNotFoundException($"Payment terminal with ID {request.Id} not found.");
            }

            if (terminal.IsDeleted || (request.BaseVersion != 0 && request.BaseVersion != terminal.Version))
            {
                throw new ZEN.Application.Services.Exceptions.SyncConflictException(
                    terminal.Id,
                    request.BaseVersion,
                    terminal.Version);
            }

            terminal.Name = request.Name;
            terminal.TerminalType = request.TerminalType;
            terminal.SerialNumber = request.SerialNumber;
            terminal.IpAddress = request.IpAddress;
            terminal.Port = request.Port;
            terminal.IsActive = request.IsActive;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new PaymentTerminalDto(
                terminal.Id,
                terminal.Name,
                terminal.TerminalType,
                terminal.SerialNumber,
                terminal.IpAddress,
                terminal.Port,
                terminal.CashDeskId,
                terminal.IsActive,
                terminal.Version,
                terminal.IsDeleted);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default, long baseVersion = 0)
        {
            var terminal = await _dbContext.PaymentTerminals.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (terminal == null)
            {
                return false;
            }

            if (terminal.IsDeleted || (baseVersion != 0 && baseVersion != terminal.Version))
            {
                throw new ZEN.Application.Services.Exceptions.SyncConflictException(
                    terminal.Id,
                    baseVersion,
                    terminal.Version);
            }

            _dbContext.PaymentTerminals.Remove(terminal);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}