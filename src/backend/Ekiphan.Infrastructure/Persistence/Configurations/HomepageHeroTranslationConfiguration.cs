using Ekiphan.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class HomepageHeroTranslationConfiguration
    : IEntityTypeConfiguration<HomepageHeroTranslation>
{
    public void Configure(EntityTypeBuilder<HomepageHeroTranslation> builder)
    {
        builder.ToTable("HomepageHeroTranslations");
        builder.HasKey(item => new { item.HomepageHeroId, item.LanguageCode });
        builder.Property(item => item.LanguageCode).HasMaxLength(2).IsUnicode(false);
        builder.Property(item => item.Title).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Subtitle).HasMaxLength(500);
        builder.Property(item => item.PrimaryCtaLabel).HasMaxLength(100).IsRequired();
        builder.Property(item => item.PrimaryCtaUrl).HasMaxLength(2048).IsUnicode(false);
        builder.Property(item => item.SecondaryCtaLabel).HasMaxLength(100);
        builder.Property(item => item.SecondaryCtaUrl).HasMaxLength(2048).IsUnicode(false);
    }
}
