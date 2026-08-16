using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZEN.Domain.Entities;

namespace ZEN.Infrastructure.Persistence.Configurations;

public class FiscalReceiptItemConfiguration : IEntityTypeConfiguration<FiscalReceiptItem>
{
    public void Configure(EntityTypeBuilder<FiscalReceiptItem> builder)
    {
        builder.ToTable("FiscalReceiptItems");

        builder.HasKey(fri => fri.Id);

        builder.Property(fri => fri.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(fri => fri.Quantity)
            .HasPrecision(18, 3);

        builder.Property(fri => fri.Price)
            .HasPrecision(18, 2);

        builder.Property(fri => fri.Total)
            .HasPrecision(18, 2);

        builder.Property(fri => fri.TaxGroupCode)
            .HasMaxLength(10);

        builder.HasOne(fri => fri.FiscalReceipt)
            .WithMany(fr => fr.Items)
            .HasForeignKey(fri => fri.FiscalReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(fri => fri.FiscalReceiptId);
    }
}
