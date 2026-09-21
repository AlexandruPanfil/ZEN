using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZEN.Domain.Entities;

namespace ZEN.Infrastructure.Persistence.Configurations;

public class MenuProductConfiguration : IEntityTypeConfiguration<MenuProduct>
{
    public void Configure(EntityTypeBuilder<MenuProduct> builder)
    {
        builder.HasKey(mp => mp.Id);

        builder.Property(mp => mp.Position).IsRequired();

        builder.HasOne(mp => mp.Menu)
            .WithMany(m => m.Products)
            .HasForeignKey(mp => mp.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mp => mp.Product)
            .WithMany()
            .HasForeignKey(mp => mp.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(mp => new { mp.MenuId, mp.ProductId }).IsUnique();
    }
}