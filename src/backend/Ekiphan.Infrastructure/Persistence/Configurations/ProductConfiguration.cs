using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.SKU)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(product => product.NormalizedSku)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();
        builder.HasIndex(product => product.SKU).IsUnique();
        builder.HasIndex(product => product.NormalizedSku).IsUnique();
        builder.HasIndex(product => product.WorkflowStatus);
        builder.HasIndex(product => product.BrandId);
        builder.HasIndex(product => product.QualityScore);
        builder.Property(product => product.RowVersion)
            .IsRowVersion();
        builder.HasIndex(product => product.ImportedByBatchId);
        builder.HasOne<Ekiphan.Domain.DataImport.ImportBatch>()
            .WithMany()
            .HasForeignKey(product => product.ImportedByBatchId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasQueryFilter(product => !product.IsDeleted);
        builder.HasOne<Brand>()
            .WithMany()
            .HasForeignKey(product => product.BrandId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(product => product.PrimaryCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(product => product.Categories)
            .WithOne()
            .HasForeignKey(category => category.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(product => product.Translations)
            .WithOne()
            .HasForeignKey(translation => translation.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(product => product.Tags)
            .WithOne()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(product => product.VariantGroups)
            .WithOne()
            .HasForeignKey(group => group.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(product => product.Variants)
            .WithOne()
            .HasForeignKey(variant => variant.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
