using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZEN.Domain.Entities;

namespace ZEN.Infrastructure.Persistence.Configurations;

public class MenuGroupReferenceConfiguration : IEntityTypeConfiguration<MenuGroupReference>
{
    public void Configure(EntityTypeBuilder<MenuGroupReference> builder)
    {
        builder.HasKey(mgr => mgr.Id);

        builder.Property(mgr => mgr.Position).IsRequired();

        builder.HasOne(mgr => mgr.Menu)
            .WithMany(m => m.GroupReferences)
            .HasForeignKey(mgr => mgr.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mgr => mgr.Group)
            .WithMany()
            .HasForeignKey(mgr => mgr.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(mgr => new { mgr.MenuId, mgr.GroupId }).IsUnique();
    }
}