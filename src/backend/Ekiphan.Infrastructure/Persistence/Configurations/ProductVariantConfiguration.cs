using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ProductVariantConfiguration
    : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");
        builder.HasKey(variant => variant.Id);
        builder.Property(variant => variant.Id).ValueGeneratedNever();
        builder.Property(variant => variant.SKU)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();
        builder.HasIndex(variant => variant.SKU).IsUnique();
        builder.HasIndex(variant => new
        {
            variant.ProductId,
            variant.SortOrder,
        });
        builder.HasOne<Ekiphan.Domain.Media.MediaAsset>()
            .WithMany()
            .HasForeignKey(variant => variant.MediaAssetId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(variant => variant.Selections)
            .WithOne()
            .HasForeignKey(selection => selection.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
