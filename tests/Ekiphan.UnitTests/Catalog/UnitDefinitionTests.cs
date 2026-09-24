using Ekiphan.Domain.Catalog;

namespace Ekiphan.UnitTests.Catalog;

public sealed class UnitDefinitionTests
{
    [Fact]
    public void BaseUnitMustHaveFactorOne()
    {
        Assert.Throws<ArgumentException>(
            () => new UnitDefinition(
                Guid.NewGuid(),
                "L",
                "L",
                "VOLUME",
                1000m,
                isBaseUnit: true));
    }

    [Fact]
    public void ValueIsNormalizedWithConversionFactor()
    {
        var litre = new UnitDefinition(
            Guid.NewGuid(),
            "L",
            "L",
            "VOLUME",
            1000m);

        Assert.Equal(80_000m, litre.Normalize(80m));
    }

    [Fact]
    public void UpdateNormalizesCodeAndChangesConversionFactor()
    {
        var unit = new UnitDefinition(
            Guid.NewGuid(),
            "cl",
            "cl",
            "VOLUME",
            10m);

        unit.Update(" ml ", "mL", 1m);

        Assert.Equal("ML", unit.Code);
        Assert.Equal("mL", unit.Symbol);
        Assert.Equal(80m, unit.Normalize(80m));
        Assert.Equal("VOLUME", unit.Dimension);
    }

    [Fact]
    public void BaseUnitUpdateStillRequiresFactorOne()
    {
        var unit = new UnitDefinition(
            Guid.NewGuid(),
            "ml",
            "mL",
            "VOLUME",
            1m,
            isBaseUnit: true);

        Assert.Throws<ArgumentException>(
            () => unit.Update("ml", "mL", 1000m));
    }
}
