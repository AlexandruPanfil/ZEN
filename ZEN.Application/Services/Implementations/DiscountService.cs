using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Interfaces;
using ZEN.Domain.Enums;

namespace ZEN.Application.Services.Implementations;

public class DiscountService : IDiscountService
{
    private readonly IRepository<Discount> _discountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DiscountService(
        IRepository<Discount> discountRepository,
        IUnitOfWork unitOfWork)
    {
        _discountRepository = discountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DiscountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var discount = await _discountRepository.GetByIdAsync(id, cancellationToken);
        return discount == null ? null : MapToDto(discount);
    }

    public async Task<DiscountDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var discount = await _discountRepository
            .FindFirstAsync(d => d.Name == code, cancellationToken);
        return discount == null ? null : MapToDto(discount);
    }

    public async Task<IEnumerable<DiscountDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var discounts = await _discountRepository.GetAllAsync(cancellationToken);
        return discounts.Select(MapToDto);
    }

    public async Task<IEnumerable<DiscountDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var discounts = await _discountRepository.GetAllAsync(cancellationToken);
        var now = DateTime.UtcNow;
        return discounts
            .Where(d => d.IsActive && (d.StartDate == null || d.StartDate <= now) && (d.EndDate == null || d.EndDate >= now))
            .Select(MapToDto);
    }

    public async Task<IEnumerable<DiscountDto>> GetByStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        return await GetAllAsync(cancellationToken);
    }

    public async Task<IEnumerable<DiscountDto>> GetActiveByStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        return await GetActiveAsync(cancellationToken);
    }

    public async Task<DiscountDto> CreateAsync(CreateDiscountRequest request, CancellationToken cancellationToken = default)
    {
        var type = Enum.TryParse<DiscountType>(request.Type, true, out var parsedType)
            ? parsedType
            : DiscountType.Percentage;

        var discount = new Discount
        {
            Name = request.Code,
            Type = type,
            Value = request.Value,
            MinimumAmount = null,
            MaxUsages = null,
            UsedCount = 0,
            StartDate = request.ValidFrom,
            EndDate = request.ValidTo,
            IsActive = request.IsActive,
        };

        await _discountRepository.AddAsync(discount, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(discount);
    }

    public async Task<DiscountDto> UpdateAsync(UpdateDiscountRequest request, CancellationToken cancellationToken = default)
    {
        var discount = await _discountRepository.GetByIdAsync(request.Id, cancellationToken);
        if (discount == null)
            throw new KeyNotFoundException("Discount not found");

        discount.Name = request.Code;
        discount.Type = Enum.TryParse<DiscountType>(request.Type, true, out var parsedType)
            ? parsedType
            : discount.Type;
        discount.Value = request.Value;
        discount.StartDate = request.ValidFrom;
        discount.EndDate = request.ValidTo;
        discount.IsActive = request.IsActive;

        _discountRepository.Update(discount);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(discount);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var discount = await _discountRepository.GetByIdAsync(id, cancellationToken);
        if (discount == null)
            return false;

        _discountRepository.Remove(discount);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<decimal> CalculateDiscountAsync(Guid discountId, decimal amount, CancellationToken cancellationToken = default)
    {
        var discount = await _discountRepository.GetByIdAsync(discountId, cancellationToken);
        if (discount == null || !discount.IsActive)
            return 0;

        var now = DateTime.UtcNow;
        if ((discount.StartDate.HasValue && discount.StartDate > now) ||
            (discount.EndDate.HasValue && discount.EndDate < now))
            return 0;

        return discount.Type switch
        {
            DiscountType.FixedAmount => Math.Min(discount.Value, amount),
            DiscountType.Percentage => amount * (discount.Value / 100m),
            _ => 0
        };
    }

    public async Task<bool> IsApplicableToProductAsync(Guid discountId, Guid productId, CancellationToken cancellationToken = default)
    {
        var discount = await _discountRepository.GetByIdAsync(discountId, cancellationToken);
        if (discount == null || !discount.IsActive)
            return false;

        return true;
    }

    private DiscountDto MapToDto(Discount discount) =>
        new DiscountDto(
            discount.Id,
            discount.Name,
            null,
            discount.Type.ToString(),
            discount.Value,
            discount.StartDate,
            discount.EndDate,
            discount.IsActive,
            []);
}
