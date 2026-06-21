using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZEN.Domain.Entities;

namespace ZEN.Infrastructure.Persistence.Configurations;

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => c.Code).IsUnique();

        builder.HasOne(c => c.Discount).WithMany(d => d.Coupons).HasForeignKey(c => c.DiscountId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(c => c.UsedBySale).WithMany().HasForeignKey(c => c.UsedBySaleId).OnDelete(DeleteBehavior.SetNull);
    }
}
