using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ProductTagConfiguration
    : IEntityTypeConfiguration<ProductTag>
{
    public void Configure(EntityTypeBuilder<ProductTag> builder)
    {
        builder.ToTable("ProductTags");
        builder.HasKey(item => new
        {
            item.ProductId,
            item.TagId,
        });
        builder.HasOne<Tag>()
            .WithMany()
            .HasForeignKey(item => item.TagId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new
        {
            item.TagId,
            item.SortOrder,
            item.ProductId,
        });
    }
}
