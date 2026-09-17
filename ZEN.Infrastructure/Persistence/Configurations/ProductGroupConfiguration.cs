using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZEN.Domain.Entities;

namespace ZEN.Infrastructure.Persistence.Configurations;

public class ProductGroupConfiguration : IEntityTypeConfiguration<ProductGroup>
{
    public void Configure(EntityTypeBuilder<ProductGroup> builder)
    {
        builder.HasKey(pg => pg.Id);

        builder.Property(pg => pg.Name).IsRequired().HasMaxLength(200);

        builder.HasOne(pg => pg.ParentGroup).WithMany(pg => pg.SubGroups).HasForeignKey(pg => pg.ParentGroupId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(pg => pg.Name);
    }
}