using Ekiphan.Domain.Content;
using Ekiphan.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class GalleryItemConfiguration : IEntityTypeConfiguration<GalleryItem>
{
    public void Configure(EntityTypeBuilder<GalleryItem> builder)
    {
        builder.ToTable("GalleryItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.CreatedAt).HasPrecision(0);
        builder.Property(item => item.UpdatedAt).HasPrecision(0);
        builder.HasIndex(item => new { item.IsPublished, item.SortOrder });
        builder.HasOne<MediaAsset>()
            .WithMany()
            .HasForeignKey(item => item.MediaAssetId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.Translations)
            .WithOne()
            .HasForeignKey(item => item.GalleryItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(item => item.Translations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
