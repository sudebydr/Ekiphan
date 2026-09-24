using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ProductRelationConfiguration
    : IEntityTypeConfiguration<ProductRelation>
{
    public void Configure(EntityTypeBuilder<ProductRelation> builder)
    {
        builder.ToTable("ProductRelations");
        builder.HasKey(relation => relation.Id);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(relation => relation.SourceProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(relation => relation.TargetProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(relation => new
        {
            relation.SourceProductId,
            relation.TargetProductId,
            relation.RelationType,
        }).IsUnique();
        builder.HasIndex(relation => new
        {
            relation.SourceProductId,
            relation.RelationType,
            relation.IsActive,
            relation.SortOrder,
        });
        builder.HasIndex(relation => new
        {
            relation.TargetProductId,
            relation.RelationType,
            relation.IsBidirectional,
            relation.IsActive,
        });
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_ProductRelations_DifferentProducts",
            "[SourceProductId] <> [TargetProductId]"));
    }
}
