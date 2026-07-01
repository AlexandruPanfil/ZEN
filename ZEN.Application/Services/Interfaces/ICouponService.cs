namespace ZEN.Application.Services.Interfaces;

public interface ICouponService
{
    Task<CouponDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CouponDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IEnumerable<CouponDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CouponDto>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<CouponDto> CreateAsync(CreateCouponRequest request, CancellationToken cancellationToken = default);
    Task<CouponDto> UpdateAsync(UpdateCouponRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<decimal> ValidateAndApplyCouponAsync(string couponCode, decimal amount, CancellationToken cancellationToken = default);
    Task<bool> RedeemCouponAsync(Guid couponId, CancellationToken cancellationToken = default);
}

public record CouponDto(
    Guid Id,
    string Code,
    string DiscountType,
    decimal DiscountValue,
    int? UsageLimit,
    int TimesUsed,
    DateTime ExpiryDate,
    string? Description,
    DateTime CreatedDate);

public record CreateCouponRequest(
    string Code,
    string DiscountType,
    decimal DiscountValue,
    int? UsageLimit,
    DateTime ExpiryDate,
    string? Description);

public record UpdateCouponRequest(
    Guid Id,
    string DiscountType,
    decimal DiscountValue,
    int? UsageLimit,
    DateTime ExpiryDate,
    string? Description);
