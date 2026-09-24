using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class BrandTranslationConfiguration
    : IEntityTypeConfiguration<BrandTranslation>
{
    public void Configure(EntityTypeBuilder<BrandTranslation> builder)
    {
        builder.ToTable("BrandTranslations");
        builder.HasKey(translation => new
        {
            translation.BrandId,
            translation.LanguageCode,
        });
        builder.Property(translation => translation.LanguageCode)
            .HasMaxLength(2)
            .IsUnicode(false);
        builder.Property(translation => translation.Description).HasMaxLength(4000);
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
