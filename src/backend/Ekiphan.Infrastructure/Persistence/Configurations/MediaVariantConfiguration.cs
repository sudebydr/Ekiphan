using Ekiphan.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class MediaVariantConfiguration : IEntityTypeConfiguration<MediaVariant>
{
    public void Configure(EntityTypeBuilder<MediaVariant> builder)
    {
        builder.ToTable("MediaVariants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MimeType).HasMaxLength(100).IsUnicode(false);
        builder.Property(x => x.Extension).HasMaxLength(16).IsUnicode(false);
        builder.Property(x => x.StorageKey).HasMaxLength(500).IsUnicode(false);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.MediaAssetId, x.VariantType }).IsUnique();
        builder.HasIndex(x => x.StorageKey).IsUnique();
        builder.HasOne<MediaAsset>().WithMany(x => x.Variants)
            .HasForeignKey(x => x.MediaAssetId).OnDelete(DeleteBehavior.Cascade);
    }
}
