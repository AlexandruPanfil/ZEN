using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZEN.Domain.Entities;

namespace ZEN.Infrastructure.Persistence.Configurations;

public class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).IsRequired().HasMaxLength(200);

        builder.HasOne(m => m.Store)
            .WithMany(s => s.Menus)
            .HasForeignKey(m => m.StoreId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(m => m.Products)
            .WithOne(mp => mp.Menu)
            .HasForeignKey(mp => mp.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Groups)
            .WithOne(mg => mg.Menu)
            .HasForeignKey(mg => mg.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.GroupReferences)
            .WithOne(mgr => mgr.Menu)
            .HasForeignKey(mgr => mgr.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.CashDesks)
            .WithOne(cd => cd.Menu!)
            .HasForeignKey(cd => cd.MenuId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}