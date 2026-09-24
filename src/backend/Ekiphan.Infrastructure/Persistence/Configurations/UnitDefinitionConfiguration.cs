using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class UnitDefinitionConfiguration
    : IEntityTypeConfiguration<UnitDefinition>
{
    public void Configure(EntityTypeBuilder<UnitDefinition> builder)
    {
        builder.ToTable("Units");
        builder.HasKey(unit => unit.Id);
        builder.Property(unit => unit.Code)
            .HasMaxLength(50)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(unit => unit.Symbol).HasMaxLength(20).IsRequired();
        builder.Property(unit => unit.Dimension)
            .HasMaxLength(50)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(unit => unit.ConversionFactorToBase)
            .HasPrecision(24, 10);
        builder.HasIndex(unit => unit.Code).IsUnique();
        builder.HasIndex(unit => unit.Dimension)
            .IsUnique()
            .HasFilter("[IsBaseUnit] = 1");
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_Units_ConversionFactorToBase",
            "[ConversionFactorToBase] > 0"));
    }
}
