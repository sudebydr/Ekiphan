using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(category => category.Id);
        builder.HasOne<ProductSection>()
            .WithMany()
            .HasForeignKey(category => category.ProductSectionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(category => category.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(category => new
        {
            category.ProductSectionId,
            category.ParentId,
            category.SortOrder,
        });
        builder.HasMany(category => category.Translations)
            .WithOne()
            .HasForeignKey(translation => translation.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
