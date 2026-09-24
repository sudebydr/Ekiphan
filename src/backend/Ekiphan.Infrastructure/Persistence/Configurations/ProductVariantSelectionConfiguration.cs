using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ProductVariantSelectionConfiguration
    : IEntityTypeConfiguration<ProductVariantSelection>
{
    public void Configure(EntityTypeBuilder<ProductVariantSelection> builder)
    {
        builder.ToTable("ProductVariantSelections");
        builder.HasKey(selection => new
        {
            selection.ProductVariantId,
            selection.VariantGroupId,
        });
        builder.HasOne<ProductVariantOption>()
            .WithMany()
            .HasForeignKey(selection => new
            {
                selection.VariantGroupId,
                selection.VariantOptionId,
            })
            .HasPrincipalKey(option => new
            {
                option.VariantGroupId,
                option.Id,
            })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(selection => selection.VariantOptionId);
    }
}
