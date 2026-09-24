using Ekiphan.Domain.Content;

namespace Ekiphan.UnitTests.Content;

public sealed class MenuItemTests
{
    [Fact]
    public void MenuItemCanBePublishedAfterTranslationIsAdded()
    {
        var item = new MenuItem(
            Guid.NewGuid(),
            " catalog ",
            MenuLocation.Header,
            "/katalog",
            false,
            false);

        item.SetTranslation("TR", "Ürün kataloğu");
        item.SetPublished(true);

        Assert.Equal("CATALOG", item.Code);
        Assert.True(item.IsPublished);
        Assert.Equal("tr", Assert.Single(item.Translations).LanguageCode);
    }

    [Fact]
    public void EmptyMenuItemCannotBePublished()
    {
        var item = new MenuItem(
            Guid.NewGuid(),
            "CATALOG",
            MenuLocation.Header,
            "/katalog",
            false,
            false);

        Assert.Throws<InvalidOperationException>(
            () => item.SetPublished(true));
    }

    [Theory]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("//malicious.example", false)]
    [InlineData("http://example.com", true)]
    [InlineData("https://user:password@example.com", true)]
    public void UnsafeUrlIsRejected(string url, bool isExternal)
    {
        Assert.Throws<ArgumentException>(
            () => new MenuItem(
                Guid.NewGuid(),
                "UNSAFE",
                MenuLocation.Header,
                url,
                isExternal,
                false));
    }

    [Fact]
    public void ExternalMenuAlwaysOpensInNewTab()
    {
        var item = new MenuItem(
            Guid.NewGuid(),
            "PARTNER",
            MenuLocation.Footer,
            "https://example.com",
            true,
            false);

        Assert.True(item.OpenInNewTab);
    }
}
