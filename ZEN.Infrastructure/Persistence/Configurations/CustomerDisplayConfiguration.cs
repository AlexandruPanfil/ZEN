using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZEN.Domain.Entities;

namespace ZEN.Infrastructure.Persistence.Configurations;

public class CustomerDisplayConfiguration : IEntityTypeConfiguration<CustomerDisplay>
{
    public void Configure(EntityTypeBuilder<CustomerDisplay> builder)
    {
        builder.HasKey(cd => cd.Id);

        builder.Property(cd => cd.Name).IsRequired().HasMaxLength(200);

        builder.HasOne(cd => cd.CashDesk)
            .WithMany()
            .HasForeignKey(cd => cd.CashDeskId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}