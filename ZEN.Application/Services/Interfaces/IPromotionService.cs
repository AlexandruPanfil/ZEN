namespace ZEN.Application.Services.Interfaces;

public interface IPromotionService
{
    Task<PromotionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<PromotionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PromotionDto>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PromotionDto>> GetByStoreAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PromotionDto>> GetActiveByStoreAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<PromotionDto> CreateAsync(CreatePromotionRequest request, CancellationToken cancellationToken = default);
    Task<PromotionDto> UpdateAsync(UpdatePromotionRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public record PromotionDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime? StartDate,
    DateTime? EndDate,
    string Status,
    List<Guid> DiscountIds,
    bool IsArchived);

public record CreatePromotionRequest(
    string Name,
    string? Description,
    DateTime? StartDate,
    DateTime? EndDate,
    string Status,
    List<Guid> DiscountIds);

public record UpdatePromotionRequest(
    Guid Id,
    string Name,
    string? Description,
    DateTime? StartDate,
    DateTime? EndDate,
    string Status,
    List<Guid> DiscountIds,
    bool IsArchived);
