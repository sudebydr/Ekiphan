using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class CategoryMediaConfiguration
    : IEntityTypeConfiguration<CategoryMedia>
{
    public void Configure(EntityTypeBuilder<CategoryMedia> builder)
    {
        builder.ToTable("CategoryMedia");
        builder.HasKey(item => new
        {
            item.CategoryId,
            item.Role,
        });
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(item => item.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MediaAsset>()
            .WithMany()
            .HasForeignKey(item => item.MediaAssetId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.MediaAssetId);
    }
}
