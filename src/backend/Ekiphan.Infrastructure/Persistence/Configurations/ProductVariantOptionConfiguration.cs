using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ProductVariantOptionConfiguration
    : IEntityTypeConfiguration<ProductVariantOption>
{
    public void Configure(EntityTypeBuilder<ProductVariantOption> builder)
    {
        builder.ToTable("ProductVariantOptions");
        builder.HasKey(option => option.Id);
        builder.Property(option => option.Id).ValueGeneratedNever();
        builder.HasAlternateKey(option => new
        {
            option.VariantGroupId,
            option.Id,
        });
        builder.Property(option => option.Code)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();
        builder.HasIndex(option => new
        {
            option.VariantGroupId,
            option.Code,
        }).IsUnique();
        builder.HasMany(option => option.Translations)
            .WithOne()
            .HasForeignKey(translation => translation.VariantOptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
