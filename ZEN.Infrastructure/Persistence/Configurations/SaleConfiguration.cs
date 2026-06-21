using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZEN.Domain.Entities;

namespace ZEN.Infrastructure.Persistence.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Number).IsRequired().HasMaxLength(50);
        builder.Property(s => s.SubTotal).HasPrecision(18, 4);
        builder.Property(s => s.TaxAmount).HasPrecision(18, 4);
        builder.Property(s => s.DiscountAmount).HasPrecision(18, 4);
        builder.Property(s => s.TotalAmount).HasPrecision(18, 4);
        builder.Property(s => s.PaidAmount).HasPrecision(18, 4);
        builder.Property(s => s.ChangeAmount).HasPrecision(18, 4);
        builder.Property(s => s.LoyaltyPointsEarned).HasPrecision(18, 2);
        builder.Property(s => s.LoyaltyPointsRedeemed).HasPrecision(18, 2);

        builder.HasIndex(s => s.Number).IsUnique();
        builder.HasIndex(s => s.Date);

        builder.HasOne(s => s.Customer).WithMany(c => c.Sales).HasForeignKey(s => s.CustomerId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(s => s.Shift).WithMany(sh => sh.Sales).HasForeignKey(s => s.ShiftId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(s => s.Store).WithMany().HasForeignKey(s => s.StoreId).OnDelete(DeleteBehavior.Restrict);
    }
}
