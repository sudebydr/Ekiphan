using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.Quotes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class QuoteRequestItemConfiguration
    : IEntityTypeConfiguration<QuoteRequestItem>
{
    public void Configure(EntityTypeBuilder<QuoteRequestItem> builder)
    {
        builder.ToTable("QuoteRequestItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ProductName).HasMaxLength(250).IsRequired();
        builder.Property(item => item.SKU)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(item => item.BrandName).HasMaxLength(150);
        builder.Property(item => item.VariantSnapshot).HasMaxLength(1000);
        builder.Property(item => item.ProductNote).HasMaxLength(2000);
        builder.Property(item => item.ImageStorageKey)
            .HasMaxLength(500)
            .IsUnicode(false);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(item => item.VariantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.ProductId);
        builder.HasIndex(item => item.VariantId);
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_QuoteRequestItems_Quantity",
            "[Quantity] > 0"));
    }
}
