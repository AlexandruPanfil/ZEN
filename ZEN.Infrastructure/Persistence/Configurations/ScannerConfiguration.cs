using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZEN.Domain.Entities;

namespace ZEN.Infrastructure.Persistence.Configurations;

public class ScannerConfiguration : IEntityTypeConfiguration<Scanner>
{
    public void Configure(EntityTypeBuilder<Scanner> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);

        builder.HasOne(s => s.CashDesk)
            .WithMany(cd => cd.Scanners)
            .HasForeignKey(s => s.CashDeskId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}