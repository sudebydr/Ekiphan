using Ekiphan.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ProductMediaImportBatchConfiguration : IEntityTypeConfiguration<ProductMediaImportBatch>
{
    public void Configure(EntityTypeBuilder<ProductMediaImportBatch> builder)
    {
        builder.ToTable("ProductMediaImportBatches"); builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.OriginalFileHash).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(x => x.FailureReason).HasMaxLength(1000);
        builder.HasIndex(x => x.CreatedByUserId); builder.HasIndex(x => x.Status); builder.HasIndex(x => x.OriginalFileHash);
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ProductMediaImportBatchItemConfiguration : IEntityTypeConfiguration<ProductMediaImportBatchItem>
{
    public void Configure(EntityTypeBuilder<ProductMediaImportBatchItem> builder)
    {
        builder.ToTable("ProductMediaImportBatchItems"); builder.HasKey(x => x.Id);
        builder.Property(x => x.TemporaryFileId).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(x => x.OriginalFileName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ExtractedSku).HasMaxLength(100); builder.Property(x => x.ContentHash).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(x => x.ErrorCode).HasMaxLength(100).IsUnicode(false); builder.Property(x => x.ErrorMessage).HasMaxLength(1000);
        builder.HasIndex(x => x.BatchId); builder.HasIndex(x => x.ProductId); builder.HasIndex(x => x.MediaAssetId);
        builder.HasIndex(x => x.ContentHash); builder.HasIndex(x => x.ExtractedSku); builder.HasIndex(x => x.Status);
        builder.HasOne<Ekiphan.Domain.Catalog.Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MediaAsset>().WithMany().HasForeignKey(x => x.MediaAssetId).OnDelete(DeleteBehavior.Restrict);
    }
}
