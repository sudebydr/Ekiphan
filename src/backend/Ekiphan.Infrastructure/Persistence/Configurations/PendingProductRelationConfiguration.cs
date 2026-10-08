using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class PendingProductRelationConfiguration : IEntityTypeConfiguration<PendingProductRelation>
{
    public void Configure(EntityTypeBuilder<PendingProductRelation> builder)
    {
        builder.ToTable("PendingProductRelations");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.TargetNormalizedSku).HasMaxLength(100).IsRequired();
        builder.HasOne<Product>().WithMany().HasForeignKey(item => item.SourceProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(item => new { item.SourceProductId, item.TargetNormalizedSku, item.RelationType }).IsUnique();
        builder.HasIndex(item => item.TargetNormalizedSku);
    }
}
