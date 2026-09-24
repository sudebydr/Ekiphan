using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class BrandMediaConfiguration
    : IEntityTypeConfiguration<BrandMedia>
{
    public void Configure(EntityTypeBuilder<BrandMedia> builder)
    {
        builder.ToTable("BrandMedia");
        builder.HasKey(item => new
        {
            item.BrandId,
            item.MediaAssetId,
            item.Role,
        });
        builder.HasOne<Brand>()
            .WithMany()
            .HasForeignKey(item => item.BrandId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MediaAsset>()
            .WithMany()
            .HasForeignKey(item => item.MediaAssetId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new
        {
            item.BrandId,
            item.Role,
            item.SortOrder,
        });
        builder.HasIndex(item => item.BrandId)
            .IsUnique()
            .HasFilter("[Role] = 1");
    }
}
