using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class AttributeTranslationConfiguration
    : IEntityTypeConfiguration<AttributeTranslation>
{
    public void Configure(EntityTypeBuilder<AttributeTranslation> builder)
    {
        builder.ToTable("AttributeTranslations");
        builder.HasKey(translation => new
        {
            translation.AttributeId,
            translation.LanguageCode,
        });
        builder.Property(translation => translation.LanguageCode)
            .HasMaxLength(2)
            .IsUnicode(false);
        builder.Property(translation => translation.Name).HasMaxLength(150).IsRequired();
    }
}
