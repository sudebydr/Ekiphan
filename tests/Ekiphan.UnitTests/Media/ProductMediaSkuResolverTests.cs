using Ekiphan.Application.MediaImport;
using Ekiphan.Infrastructure.MediaImport;

namespace Ekiphan.UnitTests.Media;

public sealed class ProductMediaSkuResolverTests
{
    [Theory]
    [InlineData("SKU-1.webp", "SKU-1")]
    [InlineData("SKU.LOGO.webp", "SKU")]
    [InlineData("SKU.L-1.webp", "SKU")]
    [InlineData("SKU.L-2.webp", "SKU")]
    [InlineData("SKU.L.webp", "SKU")]
    [InlineData("SKU.01L.webp", "SKU.01")]
    [InlineData("SKU-1.webp", "SKU")]
    [InlineData("SKU-2.webp", "SKU")]
    [InlineData("SKU_2.webp", "SKU")]
    [InlineData("SKU (2).webp", "SKU")]
    public void ResolveMatchesExactSkuOrKnownImageSuffix(string fileName, string sku)
    {
        var product = Product(sku);

        var result = new ProductMediaSkuResolver().Resolve(fileName, [product]);

        Assert.Same(product, result.Product);
        Assert.False(result.IsAmbiguous);
    }

    [Fact]
    public void ResolvePrefersExactSkuBeforeRemovingNumericSuffix()
    {
        var exact = Product("SKU-1");
        var stripped = Product("SKU");

        var result = new ProductMediaSkuResolver().Resolve("SKU-1.webp", [exact, stripped]);

        Assert.Same(exact, result.Product);
    }

    [Fact]
    public void ResolveUsesCentralSkuNormalizationForTurkishI()
    {
        var product = Product("3401.BRD.GRV06-K-I");

        var result = new ProductMediaSkuResolver().Resolve("3401.BRD.GRV06-K-İ.webp", [product]);

        Assert.Same(product, result.Product);
    }

    [Fact]
    public void ResolveDoesNotSelectAProductWhenCanonicalSkuIsAmbiguous()
    {
        var result = new ProductMediaSkuResolver().Resolve("SKU.LOGO.webp", [Product("SKU"), Product("SKU")]);

        Assert.Null(result.Product);
        Assert.True(result.IsAmbiguous);
    }

    [Fact]
    public void ResolveDoesNotUseFuzzyMatching()
    {
        var result = new ProductMediaSkuResolver().Resolve("SKU-EXTRA.webp", [Product("SKU")]);

        Assert.Null(result.Product);
        Assert.False(result.IsAmbiguous);
    }

    private static ProductMediaProductMatch Product(string sku) => new(
        Guid.NewGuid(), sku, sku, false, true, DateTimeOffset.UtcNow, false);
}
