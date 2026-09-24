using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ProductCategoryConfiguration
    : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("ProductCategories");
        builder.HasKey(category => new
        {
            category.ProductId,
            category.CategoryId,
        });
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(category => category.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(category => new
        {
            category.CategoryId,
            category.SortOrder,
        });
        builder.HasIndex(category => category.ProductId)
            .IsUnique()
            .HasFilter("[IsPrimary] = 1");
    }
}
