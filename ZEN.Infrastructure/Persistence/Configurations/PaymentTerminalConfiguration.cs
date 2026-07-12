using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZEN.Domain.Entities;

namespace ZEN.Infrastructure.Persistence.Configurations;

public class PaymentTerminalConfiguration : IEntityTypeConfiguration<PaymentTerminal>
{
    public void Configure(EntityTypeBuilder<PaymentTerminal> builder)
    {
        builder.HasKey(pt => pt.Id);

        builder.Property(pt => pt.Name).IsRequired().HasMaxLength(200);

        builder.HasOne(pt => pt.CashDesk)
            .WithMany(cd => cd.PaymentTerminals)
            .HasForeignKey(pt => pt.CashDeskId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}