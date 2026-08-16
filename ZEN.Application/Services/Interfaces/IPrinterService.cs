using ZEN.Application.DTOs.Admin;
using ZEN.Domain.Entities;
using ZEN.Domain.Enums;

namespace ZEN.Application.Services.Interfaces
{
    public interface IPrinterService
    {
        Task<PrinterDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<PrinterDto>> GetAllAsync(CancellationToken cancellationToken = default);
        IEnumerable<PrinterDto> GetAll();
        Task<IEnumerable<PrinterDto>> GetFiscalPrintersAsync(CancellationToken cancellationToken = default);
        Task<PrinterDto> CreateAsync(CreatePrinterRequest request, CancellationToken cancellationToken = default);
        Task<PrinterDto> UpdateAsync(UpdatePrinterRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }

    public record CreatePrinterRequest(
        string Name,
        string PrinterName,
        string PrinterType,
        string? ConnectionType,
        string? ConnectionString,
        bool IsDefault,
        bool IsActive,
        string? ApiKey,
        string? ApiSecret,
        string? DeviceId,
        string? PointOfSaleId,
        string? FiscalCloudBaseUrl);

    public record UpdatePrinterRequest(
        Guid Id,
        string Name,
        string PrinterName,
        string PrinterType,
        string? ConnectionType,
        string? ConnectionString,
        bool IsDefault,
        bool IsActive,
        string? ApiKey,
        string? ApiSecret,
        string? DeviceId,
        string? PointOfSaleId,
        string? FiscalCloudBaseUrl);
}