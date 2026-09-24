using Ekiphan.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class GalleryItemTranslationConfiguration
    : IEntityTypeConfiguration<GalleryItemTranslation>
{
    public void Configure(EntityTypeBuilder<GalleryItemTranslation> builder)
    {
        builder.ToTable("GalleryItemTranslations");
        builder.HasKey(item => new { item.GalleryItemId, item.LanguageCode });
        builder.Property(item => item.LanguageCode).HasMaxLength(2).IsUnicode(false);
        builder.Property(item => item.Title).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Caption).HasMaxLength(1000);
    }
}
