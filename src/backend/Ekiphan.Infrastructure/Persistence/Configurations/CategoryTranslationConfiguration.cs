using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class CategoryTranslationConfiguration
    : IEntityTypeConfiguration<CategoryTranslation>
{
    public void Configure(EntityTypeBuilder<CategoryTranslation> builder)
    {
        builder.ToTable("CategoryTranslations");
        builder.HasKey(translation => new
        {
            translation.CategoryId,
            translation.LanguageCode,
        });
        builder.Property(translation => translation.LanguageCode)
            .HasMaxLength(2)
            .IsUnicode(false);
        builder.Property(translation => translation.Name).HasMaxLength(200).IsRequired();
        builder.Property(translation => translation.Slug)
            .HasMaxLength(250)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(translation => translation.Description).HasMaxLength(4000);
        builder.Property(translation => translation.MetaTitle).HasMaxLength(70);
        builder.Property(translation => translation.MetaDescription).HasMaxLength(320);
        builder.Property(translation => translation.CanonicalUrl)
            .HasMaxLength(2048)
            .IsUnicode(false);
        builder.Property(translation => translation.OpenGraphTitle).HasMaxLength(95);
        builder.Property(translation => translation.OpenGraphDescription).HasMaxLength(300);
        builder.HasIndex(translation => new
        {
            translation.LanguageCode,
            translation.Slug,
        }).IsUnique();
    }
}
