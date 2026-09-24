using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brands");
        builder.HasKey(brand => brand.Id);
        builder.Property(brand => brand.Name).HasMaxLength(150).IsRequired();
        builder.Property(brand => brand.WebsiteUrl)
            .HasMaxLength(2048)
            .IsUnicode(false);
        builder.HasIndex(brand => brand.Name).IsUnique();
        builder.HasMany(brand => brand.Translations)
            .WithOne()
            .HasForeignKey(translation => translation.BrandId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
