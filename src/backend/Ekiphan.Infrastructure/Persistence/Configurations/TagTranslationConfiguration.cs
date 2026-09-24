using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class TagTranslationConfiguration
    : IEntityTypeConfiguration<TagTranslation>
{
    public void Configure(EntityTypeBuilder<TagTranslation> builder)
    {
        builder.ToTable("TagTranslations");
        builder.HasKey(translation => new
        {
            translation.TagId,
            translation.LanguageCode,
        });
        builder.Property(translation => translation.LanguageCode)
            .HasMaxLength(2)
            .IsUnicode(false);
        builder.Property(translation => translation.Name)
            .HasMaxLength(150)
            .IsRequired();
        builder.Property(translation => translation.Slug)
            .HasMaxLength(200)
            .IsUnicode(false)
            .IsRequired();
        builder.HasIndex(translation => new
        {
            translation.LanguageCode,
            translation.Slug,
        }).IsUnique();
        builder.HasIndex(translation => new
        {
            translation.LanguageCode,
            translation.Name,
        });
    }
}
