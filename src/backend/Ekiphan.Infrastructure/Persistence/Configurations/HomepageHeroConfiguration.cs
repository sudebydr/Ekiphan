using Ekiphan.Domain.Content;
using Ekiphan.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class HomepageHeroConfiguration
    : IEntityTypeConfiguration<HomepageHero>
{
    public void Configure(EntityTypeBuilder<HomepageHero> builder)
    {
        builder.ToTable("HomepageHeroes");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.StartsAt).HasPrecision(0);
        builder.Property(item => item.EndsAt).HasPrecision(0);
        builder.Property(item => item.CreatedAt).HasPrecision(0);
        builder.Property(item => item.UpdatedAt).HasPrecision(0);
        builder.HasIndex(item => new { item.IsPublished, item.SortOrder });
        builder.HasOne<MediaAsset>()
            .WithMany()
            .HasForeignKey(item => item.DesktopMediaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MediaAsset>()
            .WithMany()
            .HasForeignKey(item => item.MobileMediaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.Translations)
            .WithOne()
            .HasForeignKey(item => item.HomepageHeroId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(item => item.Translations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
