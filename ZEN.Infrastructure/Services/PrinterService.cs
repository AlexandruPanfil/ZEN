using ZEN.Application.DTOs.Admin;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Enums;
using ZEN.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ZEN.Infrastructure.Services
{
    public class PrinterService : IPrinterService
    {
        private readonly ZenDbContext _dbContext;

        public PrinterService(ZenDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PrinterDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var printer = await _dbContext.Printers.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            return printer == null ? null : new PrinterDto(
                printer.Id,
                printer.Name,
                printer.Type.ToString(),
                printer.FiscalDeviceType?.ToString(),
                printer.IsDefault,
                printer.IsActive,
                printer.ApiKey,
                printer.ApiSecret,
                printer.DeviceId,
                printer.PointOfSaleId,
                 printer.FiscalCloudBaseUrl,
                 printer.FiscalCloudTimeoutSeconds ?? 30,
                 printer.Version,
                 printer.IsDeleted);
        }

        public async Task<IEnumerable<PrinterDto>> GetAllAsync(
            CancellationToken cancellationToken = default,
            bool includeDeleted = false)
        {
            var query = includeDeleted ? _dbContext.Printers.IgnoreQueryFilters() : _dbContext.Printers;
            var printers = await query.ToListAsync(cancellationToken);
            return printers.Select(p => new PrinterDto(
                p.Id,
                p.Name,
                p.Type.ToString(),
                p.FiscalDeviceType?.ToString(),
                p.IsDefault,
                p.IsActive,
                p.ApiKey,
                p.ApiSecret,
                p.DeviceId,
                p.PointOfSaleId,
                 p.FiscalCloudBaseUrl,
                 p.FiscalCloudTimeoutSeconds ?? 30,
                 p.Version,
                 p.IsDeleted));
        }

        public IEnumerable<PrinterDto> GetAll()
        {
            var printers = _dbContext.Printers.ToList();
            return printers.Select(p => new PrinterDto(
                p.Id,
                p.Name,
                p.Type.ToString(),
                p.FiscalDeviceType?.ToString(),
                p.IsDefault,
                p.IsActive,
                p.ApiKey,
                p.ApiSecret,
                p.DeviceId,
                p.PointOfSaleId,
                 p.FiscalCloudBaseUrl,
                 p.FiscalCloudTimeoutSeconds ?? 30,
                 p.Version,
                 p.IsDeleted));
        }

        public async Task<IEnumerable<PrinterDto>> GetFiscalPrintersAsync(CancellationToken cancellationToken = default)
        {
            var printers = await _dbContext.Printers.Where(p => p.Type == PrinterType.Fiscal).ToListAsync(cancellationToken);
            return printers.Select(p => new PrinterDto(
                p.Id,
                p.Name,
                p.Type.ToString(),
                p.FiscalDeviceType?.ToString(),
                p.IsDefault,
                p.IsActive,
                p.ApiKey,
                p.ApiSecret,
                p.DeviceId,
                p.PointOfSaleId,
                 p.FiscalCloudBaseUrl,
                 p.FiscalCloudTimeoutSeconds ?? 30,
                 p.Version,
                 p.IsDeleted));
        }

        public async Task<PrinterDto> CreateAsync(CreatePrinterRequest request, CancellationToken cancellationToken = default)
        {
            var printer = new Printer
            {
                Id = request.Id ?? Guid.NewGuid(),
                Name = request.Name,
                Type = Enum.Parse<PrinterType>(request.PrinterType),
                FiscalDeviceType = string.IsNullOrWhiteSpace(request.FiscalDeviceType) ? null : Enum.Parse<FiscalDeviceType>(request.FiscalDeviceType),
                IsDefault = request.IsDefault,
                IsActive = request.IsActive,
                ApiKey = request.ApiKey,
                ApiSecret = request.ApiSecret,
                DeviceId = request.DeviceId,
                 PointOfSaleId = request.PointOfSaleId,
                 FiscalCloudBaseUrl = request.FiscalCloudBaseUrl,
                 FiscalCloudTimeoutSeconds = request.FiscalCloudTimeoutSeconds
             };

            _dbContext.Printers.Add(printer);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new PrinterDto(
                printer.Id,
                printer.Name,
                printer.Type.ToString(),
                printer.FiscalDeviceType?.ToString(),
                printer.IsDefault,
                printer.IsActive,
                printer.ApiKey,
                printer.ApiSecret,
                printer.DeviceId,
                printer.PointOfSaleId,
                printer.FiscalCloudBaseUrl,
                printer.FiscalCloudTimeoutSeconds ?? 30,
                printer.Version,
                printer.IsDeleted);
        }

        public async Task<PrinterDto> UpdateAsync(UpdatePrinterRequest request, CancellationToken cancellationToken = default)
        {
            var printer = await _dbContext.Printers.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            if (printer == null)
            {
                throw new KeyNotFoundException($"Printer with ID {request.Id} not found.");
            }

            if (printer.IsDeleted || (request.BaseVersion != 0 && request.BaseVersion != printer.Version))
            {
                throw new ZEN.Application.Services.Exceptions.SyncConflictException(
                    printer.Id,
                    request.BaseVersion,
                    printer.Version);
            }

            printer.Name = request.Name;
            printer.Type = Enum.Parse<PrinterType>(request.PrinterType);
            printer.FiscalDeviceType = string.IsNullOrWhiteSpace(request.FiscalDeviceType) ? null : Enum.Parse<FiscalDeviceType>(request.FiscalDeviceType);
            printer.IsDefault = request.IsDefault;
            printer.IsActive = request.IsActive;
            printer.ApiKey = request.ApiKey;
            printer.ApiSecret = request.ApiSecret;
            printer.DeviceId = request.DeviceId;
             printer.PointOfSaleId = request.PointOfSaleId;
             printer.FiscalCloudBaseUrl = request.FiscalCloudBaseUrl;
             printer.FiscalCloudTimeoutSeconds = request.FiscalCloudTimeoutSeconds;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new PrinterDto(
                printer.Id,
                printer.Name,
                printer.Type.ToString(),
                printer.FiscalDeviceType?.ToString(),
                printer.IsDefault,
                printer.IsActive,
                printer.ApiKey,
                printer.ApiSecret,
                printer.DeviceId,
                printer.PointOfSaleId,
                 printer.FiscalCloudBaseUrl,
                 printer.FiscalCloudTimeoutSeconds ?? 30,
                 printer.Version,
                 printer.IsDeleted);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default, long baseVersion = 0)
        {
            var printer = await _dbContext.Printers.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (printer == null)
            {
                return false;
            }

            if (printer.IsDeleted || (baseVersion != 0 && baseVersion != printer.Version))
            {
                throw new ZEN.Application.Services.Exceptions.SyncConflictException(
                    printer.Id,
                    baseVersion,
                    printer.Version);
            }

            _dbContext.Printers.Remove(printer);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}