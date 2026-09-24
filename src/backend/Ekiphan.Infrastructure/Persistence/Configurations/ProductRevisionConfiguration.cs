using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ProductRevisionConfiguration : IEntityTypeConfiguration<ProductRevision>
{
    public void Configure(EntityTypeBuilder<ProductRevision> builder)
    {
        builder.ToTable("ProductRevisions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.VersionNumber).IsRequired();
        builder.Property(x => x.ChangeType).IsRequired();
        builder.Property(x => x.SnapshotJson).IsRequired();
        builder.Property(x => x.CreatedByUserId).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.ProductId, x.VersionNumber }).IsUnique();
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.CreatedByUserId);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
