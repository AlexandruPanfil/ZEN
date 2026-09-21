using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZEN.Domain.Entities;

namespace ZEN.Infrastructure.Persistence.Configurations;

public class MenuGroupProductConfiguration : IEntityTypeConfiguration<MenuGroupProduct>
{
    public void Configure(EntityTypeBuilder<MenuGroupProduct> builder)
    {
        builder.HasKey(mgp => mgp.Id);

        builder.Property(mgp => mgp.Position).IsRequired();

        builder.HasOne(mgp => mgp.MenuGroup)
            .WithMany(mg => mg.Products)
            .HasForeignKey(mgp => mgp.MenuGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mgp => mgp.Product)
            .WithMany()
            .HasForeignKey(mgp => mgp.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(mgp => new { mgp.MenuGroupId, mgp.ProductId }).IsUnique();
    }
}