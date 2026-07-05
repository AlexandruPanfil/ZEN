using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Interfaces;
using ZEN.Domain.Enums;

namespace ZEN.Application.Services.Implementations;

public class CouponService : ICouponService
{
    private readonly IRepository<Coupon> _couponRepository;
    private readonly IRepository<Discount> _discountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CouponService(
        IRepository<Coupon> couponRepository,
        IRepository<Discount> discountRepository,
        IUnitOfWork unitOfWork)
    {
        _couponRepository = couponRepository;
        _discountRepository = discountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CouponDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var coupon = await _couponRepository.GetByIdAsync(id, cancellationToken);
        return coupon == null ? null : await MapToDtoAsync(coupon, cancellationToken);
    }

    public async Task<CouponDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var coupon = await _couponRepository
            .FindFirstAsync(c => c.Code == code, cancellationToken);
        return coupon == null ? null : await MapToDtoAsync(coupon, cancellationToken);
    }

    public async Task<IEnumerable<CouponDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var coupons = await _couponRepository.GetAllAsync(cancellationToken);
        var result = new List<CouponDto>();
        foreach (var coupon in coupons)
        {
            result.Add(await MapToDtoAsync(coupon, cancellationToken));
        }

        return result;
    }

    public async Task<IEnumerable<CouponDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var coupons = await _couponRepository.GetAllAsync(cancellationToken);
        var active = new List<CouponDto>();

        foreach (var coupon in coupons)
        {
            if (await IsCouponActiveAsync(coupon, cancellationToken))
            {
                active.Add(await MapToDtoAsync(coupon, cancellationToken));
            }
        }

        return active;
    }

    public async Task<CouponDto> CreateAsync(CreateCouponRequest request, CancellationToken cancellationToken = default)
    {
        // Check for duplicate code
        var existing = await _couponRepository.FindFirstAsync(c => c.Code == request.Code, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException("Coupon code already exists");

        // Fix: Explicitly map the string values coming from your Blazor frontend
        DiscountType discountType;
        if (string.Equals(request.DiscountType, "FixedAmount", StringComparison.OrdinalIgnoreCase))
        {
            discountType = DiscountType.FixedAmount; // <-- Update to match your actual backend Enum item name
        }
        else if (string.Equals(request.DiscountType, "Percentage", StringComparison.OrdinalIgnoreCase))
        {
            discountType = DiscountType.Percentage;
        }
        else
        {
            // Fallback or validation error
            throw new ArgumentException($"Invalid discount type: {request.DiscountType}");
        }

        var discount = new Discount
        {
            Name = request.Code,
            Type = discountType, // Uses the correctly mapped type
            Value = request.DiscountValue,
            MinimumAmount = null,
            MaxUsages = request.UsageLimit,
            UsedCount = 0,
            StartDate = null,
            EndDate = request.ExpiryDate,
            IsActive = true
        };

        await _discountRepository.AddAsync(discount, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var coupon = new Coupon
        {
            Code = request.Code,
            DiscountId = discount.Id,
            IsUsed = false,
            UsedBySaleId = null,
            UsedAt = null
        };

        await _couponRepository.AddAsync(coupon, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToDtoAsync(coupon, cancellationToken);
    }

    public async Task<CouponDto> UpdateAsync(UpdateCouponRequest request, CancellationToken cancellationToken = default)
    {
        var coupon = await _couponRepository.GetByIdAsync(request.Id, cancellationToken);
        if (coupon == null)
            throw new KeyNotFoundException("Coupon not found");

        var discount = await _discountRepository.GetByIdAsync(coupon.DiscountId, cancellationToken);
        if (discount == null)
            throw new KeyNotFoundException("Discount for coupon not found");

        discount.Name = coupon.Code;
        discount.Type = Enum.TryParse<DiscountType>(request.DiscountType, true, out var parsedType)
            ? parsedType
            : discount.Type;
        discount.Value = request.DiscountValue;
        discount.MaxUsages = request.UsageLimit;
        discount.EndDate = request.ExpiryDate;
        discount.IsActive = true;

        _discountRepository.Update(discount);
        _couponRepository.Update(coupon);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToDtoAsync(coupon, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var coupon = await _couponRepository.GetByIdAsync(id, cancellationToken);
        if (coupon == null)
            return false;

        _couponRepository.Remove(coupon);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<decimal> ValidateAndApplyCouponAsync(string couponCode, decimal amount, CancellationToken cancellationToken = default)
    {
        var coupon = await _couponRepository
            .FindFirstAsync(c => c.Code == couponCode, cancellationToken);

        if (coupon == null)
            throw new KeyNotFoundException("Coupon not found");

        if (!await IsCouponActiveAsync(coupon, cancellationToken))
            throw new InvalidOperationException("Coupon has expired");

        var discount = await _discountRepository.GetByIdAsync(coupon.DiscountId, cancellationToken);
        if (discount == null)
            throw new KeyNotFoundException("Discount for coupon not found");

        if (discount.MaxUsages.HasValue && discount.UsedCount >= discount.MaxUsages)
            throw new InvalidOperationException("Coupon usage limit reached");

        return discount.Type switch
        {
            DiscountType.FixedAmount => Math.Min(discount.Value, amount),
            DiscountType.Percentage => amount * (discount.Value / 100m),
            _ => 0
        };
    }

    public async Task<bool> RedeemCouponAsync(Guid couponId, CancellationToken cancellationToken = default)
    {
        var coupon = await _couponRepository.GetByIdAsync(couponId, cancellationToken);
        if (coupon == null)
            return false;

        var discount = await _discountRepository.GetByIdAsync(coupon.DiscountId, cancellationToken);
        if (discount == null)
            throw new KeyNotFoundException("Discount for coupon not found");

        if (discount.MaxUsages.HasValue && discount.UsedCount >= discount.MaxUsages)
            throw new InvalidOperationException("Coupon usage limit reached");

        discount.UsedCount++;
        coupon.IsUsed = true;
        coupon.UsedAt = DateTime.UtcNow;

        _discountRepository.Update(discount);
        _couponRepository.Update(coupon);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<bool> IsCouponActiveAsync(Coupon coupon, CancellationToken cancellationToken)
    {
        var discount = await _discountRepository.GetByIdAsync(coupon.DiscountId, cancellationToken);
        if (discount == null)
            return false;

        var now = DateTime.UtcNow;
        return discount.IsActive &&
               (discount.StartDate == null || discount.StartDate <= now) &&
               (discount.EndDate == null || discount.EndDate >= now);
    }

    private async Task<CouponDto> MapToDtoAsync(Coupon coupon, CancellationToken cancellationToken)
    {
        var discount = await _discountRepository.GetByIdAsync(coupon.DiscountId, cancellationToken);
        return new CouponDto(
            coupon.Id,
            coupon.Code,
            discount?.Type.ToString() ?? string.Empty,
            discount?.Value ?? 0,
            discount?.MaxUsages,
            discount?.UsedCount ?? 0,
            discount?.EndDate ?? DateTime.UtcNow,
            discount?.Name,
            coupon.CreatedAt);
    }
}
