using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZEN.Domain.Entities;

namespace ZEN.Infrastructure.Persistence.Configurations;

public class HeldSaleConfiguration : IEntityTypeConfiguration<HeldSale>
{
    public void Configure(EntityTypeBuilder<HeldSale> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.SerializedItems).HasColumnType("nvarchar(max)");
        builder.Property(h => h.UserId).IsRequired().HasMaxLength(450);

        builder.HasOne(h => h.Store).WithMany().HasForeignKey(h => h.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(h => h.Customer).WithMany().HasForeignKey(h => h.CustomerId).OnDelete(DeleteBehavior.SetNull);
    }
}
