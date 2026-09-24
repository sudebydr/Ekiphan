using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ProductSectionTranslationConfiguration
    : IEntityTypeConfiguration<ProductSectionTranslation>
{
    public void Configure(EntityTypeBuilder<ProductSectionTranslation> builder)
    {
        builder.ToTable("ProductSectionTranslations");
        builder.HasKey(translation => new
        {
            translation.ProductSectionId,
            translation.LanguageCode,
        });
        builder.Property(translation => translation.LanguageCode)
            .HasMaxLength(2)
            .IsUnicode(false);
        builder.Property(translation => translation.Name).HasMaxLength(150).IsRequired();
        builder.Property(translation => translation.Slug)
            .HasMaxLength(200)
            .IsUnicode(false)
            .IsRequired();
        builder.HasIndex(translation => new
        {
            translation.LanguageCode,
            translation.Slug,
        }).IsUnique();
    }
}
