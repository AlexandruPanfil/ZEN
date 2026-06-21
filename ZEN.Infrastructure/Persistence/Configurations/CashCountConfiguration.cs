using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZEN.Domain.Entities;

namespace ZEN.Infrastructure.Persistence.Configurations;

public class CashCountConfiguration : IEntityTypeConfiguration<CashCount>
{
    public void Configure(EntityTypeBuilder<CashCount> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.DenominationDetails).HasColumnType("nvarchar(max)");
        builder.Property(c => c.TotalCounted).HasPrecision(18, 4);

        builder.HasOne(c => c.Shift).WithMany(s => s.CashCounts).HasForeignKey(c => c.ShiftId).OnDelete(DeleteBehavior.Cascade);
    }
}
