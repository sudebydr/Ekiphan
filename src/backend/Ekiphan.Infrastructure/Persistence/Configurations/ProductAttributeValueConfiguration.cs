using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ProductAttributeValueConfiguration
    : IEntityTypeConfiguration<ProductAttributeValue>
{
    public void Configure(EntityTypeBuilder<ProductAttributeValue> builder)
    {
        builder.ToTable("ProductAttributeValues");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.TextValue).HasMaxLength(2000);
        builder.Property(value => value.NumericValue).HasPrecision(24, 8);
        builder.Property(value => value.RawValue).HasMaxLength(2000);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(value => value.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<AttributeDefinition>()
            .WithMany()
            .HasForeignKey(value => value.AttributeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AttributeOption>()
            .WithMany()
            .HasForeignKey(value => value.AttributeOptionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitDefinition>()
            .WithMany()
            .HasForeignKey(value => value.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => new
        {
            value.ProductId,
            value.AttributeId,
            value.Sequence,
        }).IsUnique();
        builder.HasIndex(value => new
        {
            value.AttributeId,
            value.AttributeOptionId,
            value.ProductId,
        }).HasFilter("[AttributeOptionId] IS NOT NULL");
        builder.HasIndex(value => new
        {
            value.AttributeId,
            value.NumericValue,
            value.ProductId,
        }).HasFilter("[NumericValue] IS NOT NULL");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_ProductAttributeValues_SingleTypedValue",
                "(CASE WHEN [TextValue] IS NULL THEN 0 ELSE 1 END + " +
                "CASE WHEN [NumericValue] IS NULL THEN 0 ELSE 1 END + " +
                "CASE WHEN [BooleanValue] IS NULL THEN 0 ELSE 1 END + " +
                "CASE WHEN [AttributeOptionId] IS NULL THEN 0 ELSE 1 END) = 1");
            table.HasCheckConstraint(
                "CK_ProductAttributeValues_UnitRequiresNumber",
                "[UnitId] IS NULL OR [NumericValue] IS NOT NULL");
            table.HasCheckConstraint(
                "CK_ProductAttributeValues_Sequence",
                "[Sequence] >= 0");
        });
    }
}
