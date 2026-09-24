using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ProductMediaConfiguration
    : IEntityTypeConfiguration<ProductMedia>
{
    public void Configure(EntityTypeBuilder<ProductMedia> builder)
    {
        builder.ToTable("ProductMedia");
        builder.HasKey(item => new
        {
            item.ProductId,
            item.MediaAssetId,
            item.Role,
        });
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MediaAsset>()
            .WithMany()
            .HasForeignKey(item => item.MediaAssetId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new
        {
            item.ProductId,
            item.Role,
            item.SortOrder,
        });
        builder.HasIndex(item => item.ProductId)
            .IsUnique()
            .HasFilter("[IsDefault] = 1 AND [Role] = 1");
        builder.Property(item => item.RowVersion).IsRowVersion();
        builder.HasIndex(item => item.CreatedByImportBatchId);
        builder.HasOne<ProductMediaImportBatch>()
            .WithMany()
            .HasForeignKey(item => item.CreatedByImportBatchId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_ProductMedia_DefaultRole",
            "[IsDefault] = 0 OR [Role] = 1"));
    }
}
