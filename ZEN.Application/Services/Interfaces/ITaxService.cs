namespace ZEN.Application.Services.Interfaces;

public interface ITaxService
{
    Task<TaxDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TaxDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TaxDto>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<TaxDto?> GetDefaultTaxAsync(CancellationToken cancellationToken = default);
    Task<TaxDto> CreateAsync(CreateTaxRequest request, CancellationToken cancellationToken = default);
    Task<TaxDto> UpdateAsync(UpdateTaxRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> SetDefaultTaxAsync(Guid taxId, CancellationToken cancellationToken = default);
}

public record TaxDto(Guid Id, string Name, decimal Rate, bool IsDefault, bool IsActive);

public record CreateTaxRequest(
    string Name,
    decimal Rate,
    bool IsActive = true);

public record UpdateTaxRequest(
    Guid Id,
    string Name,
    decimal Rate,
    bool IsDefault,
    bool IsActive);
