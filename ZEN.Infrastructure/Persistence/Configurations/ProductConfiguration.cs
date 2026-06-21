using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZEN.Domain.Entities;

namespace ZEN.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Barcode).HasMaxLength(100);
        builder.Property(p => p.SKU).HasMaxLength(100);
        builder.Property(p => p.CostPrice).HasPrecision(18, 4);
        builder.Property(p => p.SalePrice).HasPrecision(18, 4);
        builder.Property(p => p.Weight).HasPrecision(18, 4);

        builder.HasIndex(p => p.Barcode).IsUnique().HasFilter("[Barcode] IS NOT NULL");
        builder.HasIndex(p => p.SKU).IsUnique().HasFilter("[SKU] IS NOT NULL");
        builder.HasIndex(p => p.Name);

        builder.HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(p => p.Brand).WithMany(b => b.Products).HasForeignKey(p => p.BrandId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(p => p.Tax).WithMany(t => t.Products).HasForeignKey(p => p.TaxId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(p => p.Supplier).WithMany(s => s.Products).HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.SetNull);
    }
}
