using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class CategoryAttributeConfiguration
    : IEntityTypeConfiguration<CategoryAttributeAssignment>
{
    public void Configure(EntityTypeBuilder<CategoryAttributeAssignment> builder)
    {
        builder.ToTable("CategoryAttributes");
        builder.HasKey(item => new
        {
            item.CategoryId,
            item.AttributeId,
        });
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(item => item.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<AttributeDefinition>()
            .WithMany()
            .HasForeignKey(item => item.AttributeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new
        {
            item.CategoryId,
            item.IsFilterable,
            item.SortOrder,
        });
        builder.HasIndex(item => new
        {
            item.CategoryId,
            item.IsVisibleOnComparison,
            item.SortOrder,
        });
    }
}
