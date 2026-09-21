using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZEN.Domain.Entities;

namespace ZEN.Infrastructure.Persistence.Configurations;

public class MenuGroupConfiguration : IEntityTypeConfiguration<MenuGroup>
{
    public void Configure(EntityTypeBuilder<MenuGroup> builder)
    {
        builder.HasKey(mg => mg.Id);

        builder.Property(mg => mg.Name).IsRequired().HasMaxLength(200);
        builder.Property(mg => mg.Position).IsRequired();

        builder.HasOne(mg => mg.Menu)
            .WithMany(m => m.Groups)
            .HasForeignKey(mg => mg.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(mg => mg.Products)
            .WithOne(mgp => mgp.MenuGroup)
            .HasForeignKey(mgp => mgp.MenuGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(mg => new { mg.MenuId, mg.Position });
    }
}