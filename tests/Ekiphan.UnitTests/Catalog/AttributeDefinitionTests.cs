using Ekiphan.Domain.Catalog;

namespace Ekiphan.UnitTests.Catalog;

public sealed class AttributeDefinitionTests
{
    [Fact]
    public void NonNumericAttributeCannotDefineUnitDimension()
    {
        Assert.Throws<ArgumentException>(
            () => new AttributeDefinition(
                Guid.NewGuid(),
                "material",
                AttributeDataType.Option,
                "mass"));
    }

    [Fact]
    public void TextAttributeCannotContainOptions()
    {
        var attribute = new AttributeDefinition(
            Guid.NewGuid(),
            "description",
            AttributeDataType.Text);

        Assert.Throws<InvalidOperationException>(
            () => attribute.AddOption(Guid.NewGuid(), "SHORT"));
    }

    [Fact]
    public void OptionCodesAreUniqueAfterNormalization()
    {
        var attribute = new AttributeDefinition(
            Guid.NewGuid(),
            "material",
            AttributeDataType.Option);
        attribute.AddOption(Guid.NewGuid(), " steel ");

        Assert.Throws<InvalidOperationException>(
            () => attribute.AddOption(Guid.NewGuid(), "STEEL"));
    }

    [Fact]
    public void OptionCanHaveTurkishAndEnglishTranslations()
    {
        var attribute = new AttributeDefinition(
            Guid.NewGuid(),
            "material",
            AttributeDataType.Option);
        var option = attribute.AddOption(Guid.NewGuid(), "STEEL");

        option.AddTranslation("tr", "Çelik");
        option.AddTranslation("en", "Steel");

        Assert.Equal(2, option.Translations.Count);
    }
}
