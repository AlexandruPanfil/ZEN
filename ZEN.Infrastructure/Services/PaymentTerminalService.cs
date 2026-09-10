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
            var terminal = await _dbContext.PaymentTerminals.FindAsync([id], cancellationToken);
            return terminal == null ? null : new PaymentTerminalDto(
                terminal.Id,
                terminal.Name,
                terminal.TerminalType,
                terminal.SerialNumber,
                terminal.IpAddress,
                terminal.Port,
                terminal.CashDeskId,
                terminal.IsActive);
        }

        public async Task<IEnumerable<PaymentTerminalDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var terminals = await _dbContext.PaymentTerminals.ToListAsync(cancellationToken);
            return terminals.Select(t => new PaymentTerminalDto(
                t.Id,
                t.Name,
                t.TerminalType,
                t.SerialNumber,
                t.IpAddress,
                t.Port,
                t.CashDeskId,
                t.IsActive));
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
                t.IsActive));
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
                terminal.IsActive);
        }

        public async Task<PaymentTerminalDto> UpdateAsync(UpdatePaymentTerminalRequest request, CancellationToken cancellationToken = default)
        {
            var terminal = await _dbContext.PaymentTerminals.FindAsync([request.Id], cancellationToken);
            if (terminal == null)
            {
                throw new KeyNotFoundException($"Payment terminal with ID {request.Id} not found.");
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
                terminal.IsActive);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var terminal = await _dbContext.PaymentTerminals.FindAsync([id], cancellationToken);
            if (terminal == null)
            {
                return false;
            }

            _dbContext.PaymentTerminals.Remove(terminal);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}