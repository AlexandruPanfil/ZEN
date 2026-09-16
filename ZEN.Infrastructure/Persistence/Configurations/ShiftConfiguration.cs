using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZEN.Domain.Entities;

namespace ZEN.Infrastructure.Persistence.Configurations;

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId).IsRequired().HasMaxLength(450);
        builder.Property(s => s.OpenTime).IsRequired();
        builder.Property(s => s.OpeningCash).HasPrecision(18, 4);
        builder.Property(s => s.ClosingCash).HasPrecision(18, 4);
        builder.Property(s => s.Notes).HasMaxLength(1000);

        builder.HasIndex(s => s.StoreId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.CashDeskId);

        builder.HasOne(s => s.Store)
            .WithMany(st => st.Shifts)
            .HasForeignKey(s => s.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.CashDesk)
            .WithMany()
            .HasForeignKey(s => s.CashDeskId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}