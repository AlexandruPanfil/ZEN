namespace ZEN.Application.Services.Interfaces;

public interface IDiscountService
{
    Task<DiscountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DiscountDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IEnumerable<DiscountDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<DiscountDto>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<DiscountDto>> GetByStoreAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<DiscountDto>> GetActiveByStoreAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<DiscountDto> CreateAsync(CreateDiscountRequest request, CancellationToken cancellationToken = default);
    Task<DiscountDto> UpdateAsync(UpdateDiscountRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<decimal> CalculateDiscountAsync(Guid discountId, decimal amount, CancellationToken cancellationToken = default);
    Task<bool> IsApplicableToProductAsync(Guid discountId, Guid productId, CancellationToken cancellationToken = default);
}

public record DiscountDto(
    Guid Id,
    string Code,
    string? Description,
    string Type,
    decimal Value,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    bool IsActive,
    List<string> Conditions);

public record CreateDiscountRequest(
    string Code,
    string? Description,
    string Type,
    decimal Value,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    bool IsActive,
    List<string> Conditions);

public record UpdateDiscountRequest(
    Guid Id,
    string Code,
    string? Description,
    string Type,
    decimal Value,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    bool IsActive,
    List<string> Conditions);
