using Ekiphan.Infrastructure.Media;

namespace Ekiphan.UnitTests.Media;

public sealed class PublicMediaUrlResolverTests
{
    [Fact]
    public void SafeStorageKeyIsResolvedBelowHttpsBasePath()
    {
        var resolver = new PublicMediaUrlResolver(
            "https://cdn.example.com/ekiphan");

        var result = resolver.Resolve(
            "products/servis-tabagi/default.webp");

        Assert.Equal(
            "https://cdn.example.com/ekiphan/products/servis-tabagi/default.webp",
            result);
    }

    [Fact]
    public void SafeStorageKeyIsResolvedBelowLocalHttpBasePath()
    {
        var resolver = new PublicMediaUrlResolver("http://localhost:5175/");

        Assert.Equal(
            "http://localhost:5175/media/products/default.webp",
            resolver.Resolve("media/products/default.webp"));
    }

    [Theory]
    [InlineData("http://cdn.example.com")]
    [InlineData("https://cdn.example.com/media?token=secret")]
    [InlineData("not-a-url")]
    [InlineData("")]
    public void UnsafeOrMissingBaseUrlDisablesResolution(string baseUrl)
    {
        var resolver = new PublicMediaUrlResolver(baseUrl);

        Assert.Null(resolver.Resolve("products/default.webp"));
    }

    [Theory]
    [InlineData("../secret.webp")]
    [InlineData("/absolute/image.webp")]
    [InlineData("products\\image.webp")]
    [InlineData("products/image with space.webp")]
    [InlineData("products/image?.webp")]
    [InlineData("")]
    public void UnsafeStorageKeyIsNeverResolved(string storageKey)
    {
        var resolver = new PublicMediaUrlResolver(
            "https://cdn.example.com/media/");

        Assert.Null(resolver.Resolve(storageKey));
    }
}
