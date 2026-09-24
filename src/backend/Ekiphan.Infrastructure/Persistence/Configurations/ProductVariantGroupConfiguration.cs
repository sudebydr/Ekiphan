using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ProductVariantGroupConfiguration
    : IEntityTypeConfiguration<ProductVariantGroup>
{
    public void Configure(EntityTypeBuilder<ProductVariantGroup> builder)
    {
        builder.ToTable("ProductVariantGroups");
        builder.HasKey(group => group.Id);
        builder.Property(group => group.Id).ValueGeneratedNever();
        builder.Property(group => group.Code)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();
        builder.HasIndex(group => new
        {
            group.ProductId,
            group.Code,
        }).IsUnique();
        builder.HasIndex(group => new
        {
            group.ProductId,
            group.SortOrder,
        });
        builder.HasMany(group => group.Translations)
            .WithOne()
            .HasForeignKey(translation => translation.VariantGroupId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(group => group.Options)
            .WithOne()
            .HasForeignKey(option => option.VariantGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
