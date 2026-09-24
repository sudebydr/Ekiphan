using Ekiphan.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ContentPageTranslationConfiguration
    : IEntityTypeConfiguration<ContentPageTranslation>
{
    public void Configure(EntityTypeBuilder<ContentPageTranslation> builder)
    {
        builder.ToTable("ContentPageTranslations");
        builder.HasKey(item => new
        {
            item.ContentPageId,
            item.LanguageCode,
        });
        builder.Property(item => item.LanguageCode)
            .HasMaxLength(2)
            .IsUnicode(false);
        builder.Property(item => item.Title).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Slug)
            .HasMaxLength(200)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(item => item.Summary).HasMaxLength(500);
        builder.Property(item => item.Body).HasMaxLength(50_000).IsRequired();
        builder.Property(item => item.MetaTitle).HasMaxLength(70);
        builder.Property(item => item.MetaDescription).HasMaxLength(170);
        builder.Property(item => item.CanonicalUrl)
            .HasMaxLength(2048)
            .IsUnicode(false);
        builder.Property(item => item.OpenGraphTitle).HasMaxLength(95);
        builder.Property(item => item.OpenGraphDescription).HasMaxLength(300);
        builder.HasIndex(item => new
        {
            item.LanguageCode,
            item.Slug,
        }).IsUnique();
    }
}
