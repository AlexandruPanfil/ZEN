using ZEN.Application.DTOs.Admin;

namespace ZEN.Application.Services.Interfaces;

public interface IScannerService
{
    Task<ScannerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ScannerDto>> GetAllAsync(CancellationToken cancellationToken = default);
    IEnumerable<ScannerDto> GetAll();
    Task<ScannerDto> CreateAsync(CreateScannerRequest request, CancellationToken cancellationToken = default);
    Task<ScannerDto> UpdateAsync(UpdateScannerRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public record CreateScannerRequest(
    string Name,
    string ScannerType,
    string? ConnectionType,
    string? ConnectionString,
    bool IsActive);

public record UpdateScannerRequest(
    Guid Id,
    string Name,
    string ScannerType,
    string? ConnectionType,
    string? ConnectionString,
    bool IsActive);