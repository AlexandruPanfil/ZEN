using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZEN.Domain.Entities;

namespace ZEN.Infrastructure.Persistence.Configurations;

public class CashDeskConfiguration : IEntityTypeConfiguration<CashDesk>
{
    public void Configure(EntityTypeBuilder<CashDesk> builder)
    {
        builder.HasKey(cd => cd.Id);

        builder.Property(cd => cd.Name).IsRequired().HasMaxLength(200);

        builder.HasOne(cd => cd.Store)
            .WithMany(s => s.CashDesks)
            .HasForeignKey(cd => cd.StoreId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(cd => cd.FiscalPrinter)
            .WithMany()
            .HasForeignKey(cd => cd.FiscalPrinterId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(cd => cd.CustomerDisplay)
            .WithMany()
            .HasForeignKey(cd => cd.CustomerDisplayId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(cd => cd.PaymentTerminals)
            .WithOne(pt => pt.CashDesk!)
            .HasForeignKey(pt => pt.CashDeskId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(cd => cd.Scanners)
            .WithOne(s => s.CashDesk!)
            .HasForeignKey(s => s.CashDeskId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}