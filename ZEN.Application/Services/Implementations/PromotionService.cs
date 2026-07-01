using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Interfaces;
using ZEN.Domain.Enums;

namespace ZEN.Application.Services.Implementations;

public class PromotionService : IPromotionService
{
    private readonly IRepository<Promotion> _promotionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PromotionService(
        IRepository<Promotion> promotionRepository,
        IUnitOfWork unitOfWork)
    {
        _promotionRepository = promotionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PromotionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var promotion = await _promotionRepository.GetByIdAsync(id, cancellationToken);
        return promotion == null ? null : MapToDto(promotion);
    }

    public async Task<IEnumerable<PromotionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var promotions = await _promotionRepository.GetAllAsync(cancellationToken);
        return promotions.Select(MapToDto);
    }

    public async Task<IEnumerable<PromotionDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var promotions = await _promotionRepository.GetAllAsync(cancellationToken);
        var now = DateTime.UtcNow;
        return promotions
            .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
            .Select(MapToDto);
    }

    public async Task<IEnumerable<PromotionDto>> GetByStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        return await GetAllAsync(cancellationToken);
    }

    public async Task<IEnumerable<PromotionDto>> GetActiveByStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        return await GetActiveAsync(cancellationToken);
    }

    public async Task<PromotionDto> CreateAsync(CreatePromotionRequest request, CancellationToken cancellationToken = default)
    {
        var promotion = new Promotion
        {
            Name = request.Name,
            Description = request.Description,
            StartDate = request.StartDate ?? DateTime.UtcNow,
            EndDate = request.EndDate ?? DateTime.UtcNow,
            DiscountId = request.DiscountIds.FirstOrDefault(),
            AppliesTo = PromotionAppliesTo.All,
            ReferenceId = null,
            IsActive = request.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)
        };

        await _promotionRepository.AddAsync(promotion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(promotion);
    }

    public async Task<PromotionDto> UpdateAsync(UpdatePromotionRequest request, CancellationToken cancellationToken = default)
    {
        var promotion = await _promotionRepository.GetByIdAsync(request.Id, cancellationToken);
        if (promotion == null)
            throw new KeyNotFoundException("Promotion not found");

        promotion.Name = request.Name;
        promotion.Description = request.Description;
        promotion.StartDate = request.StartDate ?? promotion.StartDate;
        promotion.EndDate = request.EndDate ?? promotion.EndDate;
        promotion.DiscountId = request.DiscountIds.FirstOrDefault();
        promotion.IsActive = request.Status.Equals("Active", StringComparison.OrdinalIgnoreCase);

        _promotionRepository.Update(promotion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(promotion);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var promotion = await _promotionRepository.GetByIdAsync(id, cancellationToken);
        if (promotion == null)
            return false;

        _promotionRepository.Remove(promotion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private PromotionDto MapToDto(Promotion promotion) =>
        new PromotionDto(
            promotion.Id,
            promotion.Name,
            promotion.Description,
            promotion.StartDate,
            promotion.EndDate,
            promotion.IsActive ? "Active" : "Inactive",
            promotion.DiscountId == Guid.Empty ? [] : [promotion.DiscountId],
            !promotion.IsActive);
}
