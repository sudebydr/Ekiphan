using Ekiphan.Domain.Catalog;

namespace Ekiphan.UnitTests.Catalog;

public sealed class ProductAttributeValueTests
{
    [Fact]
    public void NumericValuePreservesUnitAndRawImportValue()
    {
        var unitId = Guid.NewGuid();
        var value = ProductAttributeValue.FromNumber(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            80m,
            unitId,
            rawValue: "80 litre");

        Assert.Equal(80m, value.NumericValue);
        Assert.Equal(unitId, value.UnitId);
        Assert.Equal("80 litre", value.RawValue);
    }

    [Fact]
    public void MultiValueSequenceCannotBeNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ProductAttributeValue.FromOption(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                sequence: -1));
    }

    [Fact]
    public void EmptyOptionIdentifierIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => ProductAttributeValue.FromOption(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.Empty));
    }
}
