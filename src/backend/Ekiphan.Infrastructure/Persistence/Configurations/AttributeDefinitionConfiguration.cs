using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class AttributeDefinitionConfiguration
    : IEntityTypeConfiguration<AttributeDefinition>
{
    public void Configure(EntityTypeBuilder<AttributeDefinition> builder)
    {
        builder.ToTable("Attributes");
        builder.HasKey(attribute => attribute.Id);
        builder.Property(attribute => attribute.Code)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(attribute => attribute.UnitDimension)
            .HasMaxLength(50)
            .IsUnicode(false);
        builder.HasIndex(attribute => attribute.Code).IsUnique();
        builder.HasMany(attribute => attribute.Translations)
            .WithOne()
            .HasForeignKey(translation => translation.AttributeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(attribute => attribute.Options)
            .WithOne()
            .HasForeignKey(option => option.AttributeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
