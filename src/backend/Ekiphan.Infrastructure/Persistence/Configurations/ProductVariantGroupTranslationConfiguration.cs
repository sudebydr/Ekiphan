using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ProductVariantGroupTranslationConfiguration
    : IEntityTypeConfiguration<ProductVariantGroupTranslation>
{
    public void Configure(
        EntityTypeBuilder<ProductVariantGroupTranslation> builder)
    {
        builder.ToTable("ProductVariantGroupTranslations");
        builder.HasKey(translation => new
        {
            translation.VariantGroupId,
            translation.LanguageCode,
        });
        builder.Property(translation => translation.LanguageCode)
            .HasMaxLength(2)
            .IsUnicode(false);
        builder.Property(translation => translation.Name).HasMaxLength(150).IsRequired();
    }
}
