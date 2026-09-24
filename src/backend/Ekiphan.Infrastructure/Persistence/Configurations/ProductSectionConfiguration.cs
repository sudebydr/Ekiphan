using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ProductSectionConfiguration : IEntityTypeConfiguration<ProductSection>
{
    public void Configure(EntityTypeBuilder<ProductSection> builder)
    {
        builder.ToTable("ProductSections");
        builder.HasKey(section => section.Id);
        builder.Property(section => section.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(section => section.Code).IsUnique();
        builder.HasMany(section => section.Translations)
            .WithOne()
            .HasForeignKey(translation => translation.ProductSectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
