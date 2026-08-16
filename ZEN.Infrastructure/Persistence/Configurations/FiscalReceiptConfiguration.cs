using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZEN.Domain.Entities;

namespace ZEN.Infrastructure.Persistence.Configurations;

public class FiscalReceiptConfiguration : IEntityTypeConfiguration<FiscalReceipt>
{
    public void Configure(EntityTypeBuilder<FiscalReceipt> builder)
    {
        builder.ToTable("FiscalReceipts");

        builder.HasKey(fr => fr.Id);

        builder.Property(fr => fr.FiscalReceiptNumber)
            .HasMaxLength(50);

        builder.Property(fr => fr.OperationType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(fr => fr.RequestJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(fr => fr.ResponseJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(fr => fr.ErrorMessage)
            .HasMaxLength(500);

        builder.HasOne(fr => fr.Sale)
            .WithMany(s => s.FiscalReceipts)
            .HasForeignKey(fr => fr.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(fr => fr.Items)
            .WithOne(fri => fri.FiscalReceipt)
            .HasForeignKey(fri => fri.FiscalReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(fr => fr.SaleId);
        builder.HasIndex(fr => fr.FiscalReceiptNumber);
        builder.HasIndex(fr => fr.CreatedAt);
    }
}
