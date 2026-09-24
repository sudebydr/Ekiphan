using Ekiphan.Domain.Catalog;

namespace Ekiphan.UnitTests.Catalog;

public sealed class BrandTests
{
    [Fact]
    public void WebsiteMustUseHttps()
    {
        Assert.Throws<ArgumentException>(
            () => new Brand(Guid.NewGuid(), "Sample", "http://example.com"));
    }

    [Fact]
    public void WebsiteCannotContainUserInformation()
    {
        Assert.Throws<ArgumentException>(
            () => new Brand(
                Guid.NewGuid(),
                "Sample",
                "https://user:password@example.com"));
    }

    [Fact]
    public void AbsoluteHttpsWebsiteIsAccepted()
    {
        var brand = new Brand(Guid.NewGuid(), "Sample", "https://example.com");

        Assert.Equal("https://example.com/", brand.WebsiteUrl);
    }

    [Fact]
    public void BrandAndTranslationCanBeUpdated()
    {
        var brand = new Brand(Guid.NewGuid(), "Old");
        brand.AddTranslation("tr", "Eski açıklama", "eski");

        brand.Update("New", "https://new.example", 4);
        brand.SetTranslation("TR", "Yeni açıklama", "yeni");

        var translation = Assert.Single(brand.Translations);
        Assert.Equal("New", brand.Name);
        Assert.Equal(4, brand.SortOrder);
        Assert.Equal("Yeni açıklama", translation.Description);
        Assert.Equal("yeni", translation.Slug);
    }

    [Fact]
    public void BrandCannotRemoveItsOnlyTranslation()
    {
        var brand = new Brand(Guid.NewGuid(), "Sample");
        brand.AddTranslation("tr", "Açıklama", "sample");

        Assert.Throws<InvalidOperationException>(
            () => brand.RemoveTranslation("tr"));
    }
}
