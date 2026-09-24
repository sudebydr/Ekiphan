using Ekiphan.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class MediaAssetConfiguration
    : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("MediaAssets");
        builder.HasKey(asset => asset.Id);
        builder.Property(asset => asset.OriginalFileName).HasMaxLength(260);
        builder.Property(asset => asset.StorageKey)
            .HasMaxLength(500)
            .IsUnicode(false);
        builder.Property(asset => asset.MimeType)
            .HasMaxLength(150)
            .IsUnicode(false);
        builder.Property(asset => asset.Sha256Checksum)
            .HasMaxLength(64)
            .IsUnicode(false);
        builder.Property(asset => asset.ExternalUrl)
            .HasMaxLength(2048)
            .IsUnicode(false);
        builder.Property(asset => asset.OriginalExtension).HasMaxLength(16).IsUnicode(false);
        builder.Property(asset => asset.StorageProvider).HasMaxLength(50).IsUnicode(false);
        builder.Property(asset => asset.ProcessingErrorCode).HasMaxLength(100).IsUnicode(false);
        builder.Property(asset => asset.ProcessingErrorMessage).HasMaxLength(1000);
        builder.Property(asset => asset.RowVersion).IsRowVersion();
        builder.HasIndex(asset => asset.StorageKey)
            .IsUnique()
            .HasFilter("[StorageKey] IS NOT NULL");
        builder.HasIndex(asset => asset.Sha256Checksum)
            .IsUnique()
            .HasFilter("[Sha256Checksum] IS NOT NULL");
        builder.HasIndex(asset => asset.ProcessingStatus);
        builder.HasIndex(asset => asset.CreatedAt);
        builder.HasIndex(asset => new
        {
            asset.AssetType,
            asset.Status,
            asset.CreatedAt,
        });
        builder.HasMany(asset => asset.Translations)
            .WithOne()
            .HasForeignKey(translation => translation.MediaAssetId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_MediaAssets_FileOrExternal",
                "([AssetType] = 4 AND [ExternalUrl] IS NOT NULL AND " +
                "[StorageKey] IS NULL AND [MimeType] IS NULL AND " +
                "[FileSizeBytes] IS NULL AND [Sha256Checksum] IS NULL) OR " +
                "([AssetType] <> 4 AND [ExternalUrl] IS NULL AND " +
                "[StorageKey] IS NOT NULL AND [MimeType] IS NOT NULL AND " +
                "[FileSizeBytes] IS NOT NULL AND [Sha256Checksum] IS NOT NULL)");
            table.HasCheckConstraint(
                "CK_MediaAssets_FileSize",
                "[FileSizeBytes] IS NULL OR [FileSizeBytes] > 0");
            table.HasCheckConstraint(
                "CK_MediaAssets_ArchiveState",
                "([Status] = 2 AND [ArchivedAt] IS NOT NULL) OR " +
                "([Status] <> 2 AND [ArchivedAt] IS NULL)");
        });
    }
}
