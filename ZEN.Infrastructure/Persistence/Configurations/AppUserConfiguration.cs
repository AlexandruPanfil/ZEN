using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZEN.Infrastructure.Identity;

namespace ZEN.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.HasOne(u => u.CashDesk)
            .WithMany()
            .HasForeignKey(u => u.CashDeskId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}