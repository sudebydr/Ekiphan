using Ekiphan.Domain.Content;
using Ekiphan.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class PressReleaseConfiguration : IEntityTypeConfiguration<PressRelease>
{
    public void Configure(EntityTypeBuilder<PressRelease> builder)
    {
        builder.ToTable("PressReleases");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.PublishedAt).HasPrecision(0);
        builder.Property(item => item.CreatedAt).HasPrecision(0);
        builder.Property(item => item.UpdatedAt).HasPrecision(0);
        builder.HasIndex(item => new { item.IsPublished, item.PublishedAt });
        builder.HasOne<MediaAsset>().WithMany().HasForeignKey(item => item.CoverMediaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MediaAsset>().WithMany().HasForeignKey(item => item.AttachmentMediaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.Translations).WithOne()
            .HasForeignKey(item => item.PressReleaseId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(item => item.Translations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
