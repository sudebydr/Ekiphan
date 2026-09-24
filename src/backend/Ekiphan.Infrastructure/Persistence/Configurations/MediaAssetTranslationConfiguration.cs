using Ekiphan.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class MediaAssetTranslationConfiguration
    : IEntityTypeConfiguration<MediaAssetTranslation>
{
    public void Configure(EntityTypeBuilder<MediaAssetTranslation> builder)
    {
        builder.ToTable("MediaAssetTranslations");
        builder.HasKey(translation => new
        {
            translation.MediaAssetId,
            translation.LanguageCode,
        });
        builder.Property(translation => translation.LanguageCode)
            .HasMaxLength(2)
            .IsUnicode(false);
        builder.Property(translation => translation.Title).HasMaxLength(250).IsRequired();
        builder.Property(translation => translation.AltText).HasMaxLength(500);
        builder.Property(translation => translation.Description).HasMaxLength(2000);
    }
}
