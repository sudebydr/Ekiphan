using Ekiphan.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class MenuItemTranslationConfiguration
    : IEntityTypeConfiguration<MenuItemTranslation>
{
    public void Configure(EntityTypeBuilder<MenuItemTranslation> builder)
    {
        builder.ToTable("MenuItemTranslations");
        builder.HasKey(item => new
        {
            item.MenuItemId,
            item.LanguageCode,
        });
        builder.Property(item => item.LanguageCode)
            .HasMaxLength(2)
            .IsUnicode(false);
        builder.Property(item => item.Label).HasMaxLength(100).IsRequired();
    }
}

