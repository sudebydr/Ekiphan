using Ekiphan.Domain.Catalog;

namespace Ekiphan.UnitTests.Catalog;

public sealed class ProductVariantTests
{
    [Fact]
    public void ProductCanContainAtMostTwoVariantGroups()
    {
        var product = new Product(Guid.NewGuid(), "PRODUCT-1");
        product.AddVariantGroup(Guid.NewGuid(), "COLOR");
        product.AddVariantGroup(Guid.NewGuid(), "SIZE");

        Assert.Throws<InvalidOperationException>(
            () => product.AddVariantGroup(Guid.NewGuid(), "MATERIAL"));
    }

    [Fact]
    public void VariantMustSelectOneOptionFromEveryGroup()
    {
        var product = new Product(Guid.NewGuid(), "PRODUCT-1");
        var color = product.AddVariantGroup(Guid.NewGuid(), "COLOR");
        color.AddOption(Guid.NewGuid(), "BLACK");
        product.AddVariantGroup(Guid.NewGuid(), "SIZE")
            .AddOption(Guid.NewGuid(), "LARGE");

        Assert.Throws<InvalidOperationException>(
            () => product.AddVariant(
                Guid.NewGuid(),
                "VARIANT-1",
                new Dictionary<Guid, Guid>
                {
                    [color.Id] = color.Options.Single().Id,
                }));
    }

    [Fact]
    public void SelectedOptionMustBelongToSelectedGroup()
    {
        var product = new Product(Guid.NewGuid(), "PRODUCT-1");
        var color = product.AddVariantGroup(Guid.NewGuid(), "COLOR");
        color.AddOption(Guid.NewGuid(), "BLACK");

        Assert.Throws<InvalidOperationException>(
            () => product.AddVariant(
                Guid.NewGuid(),
                "VARIANT-1",
                new Dictionary<Guid, Guid>
                {
                    [color.Id] = Guid.NewGuid(),
                }));
    }

    [Fact]
    public void DuplicateVariantCombinationIsRejected()
    {
        var product = new Product(Guid.NewGuid(), "PRODUCT-1");
        var color = product.AddVariantGroup(Guid.NewGuid(), "COLOR");
        var black = color.AddOption(Guid.NewGuid(), "BLACK");
        var selection = new Dictionary<Guid, Guid>
        {
            [color.Id] = black.Id,
        };

        product.AddVariant(Guid.NewGuid(), "VARIANT-1", selection);

        Assert.Throws<InvalidOperationException>(
            () => product.AddVariant(Guid.NewGuid(), "VARIANT-2", selection));
    }

    [Fact]
    public void VariantSkuIsNormalized()
    {
        var product = new Product(Guid.NewGuid(), "PRODUCT-1");
        var color = product.AddVariantGroup(Guid.NewGuid(), "COLOR");
        var black = color.AddOption(Guid.NewGuid(), "BLACK");

        var variant = product.AddVariant(
            Guid.NewGuid(),
            " variant-black ",
            new Dictionary<Guid, Guid>
            {
                [color.Id] = black.Id,
            });

        Assert.Equal("VARIANT-BLACK", variant.SKU);
    }

    [Fact]
    public void VariantCanBeUpdatedWithoutCreatingDuplicateCombination()
    {
        var product = new Product(Guid.NewGuid(), "PRODUCT-1");
        var color = product.AddVariantGroup(Guid.NewGuid(), "COLOR");
        var black = color.AddOption(Guid.NewGuid(), "BLACK");
        var white = color.AddOption(Guid.NewGuid(), "WHITE");
        var variant = product.AddVariant(
            Guid.NewGuid(),
            "BLACK-SKU",
            new Dictionary<Guid, Guid> { [color.Id] = black.Id });

        product.UpdateVariant(
            variant.Id,
            " white-sku ",
            new Dictionary<Guid, Guid> { [color.Id] = white.Id },
            2,
            false);

        Assert.Equal("WHITE-SKU", variant.SKU);
        Assert.False(variant.IsActive);
        Assert.Equal(
            white.Id,
            Assert.Single(variant.Selections).VariantOptionId);
    }

    [Fact]
    public void GroupsCannotBeAddedAfterVariantSkusExist()
    {
        var product = new Product(Guid.NewGuid(), "PRODUCT-1");
        var color = product.AddVariantGroup(Guid.NewGuid(), "COLOR");
        var black = color.AddOption(Guid.NewGuid(), "BLACK");
        product.AddVariant(
            Guid.NewGuid(),
            "BLACK-SKU",
            new Dictionary<Guid, Guid> { [color.Id] = black.Id });

        Assert.Throws<InvalidOperationException>(
            () => product.AddVariantGroup(Guid.NewGuid(), "SIZE"));
    }

    [Fact]
    public void VariantCanStoreAndReplaceAnOptionalImageReference()
    {
        var product = new Product(Guid.NewGuid(), "PRODUCT-1");
        var color = product.AddVariantGroup(Guid.NewGuid(), "COLOR");
        var black = color.AddOption(Guid.NewGuid(), "BLACK");
        var firstImageId = Guid.NewGuid();
        var replacementImageId = Guid.NewGuid();
        var selections = new Dictionary<Guid, Guid>
        {
            [color.Id] = black.Id,
        };

        var variant = product.AddVariant(
            Guid.NewGuid(),
            "BLACK-SKU",
            selections,
            mediaAssetId: firstImageId);
        product.UpdateVariant(
            variant.Id,
            variant.SKU,
            selections,
            0,
            true,
            replacementImageId);

        Assert.Equal(replacementImageId, variant.MediaAssetId);
    }
}
