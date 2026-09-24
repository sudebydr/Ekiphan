using Ekiphan.Domain.Common;

namespace Ekiphan.UnitTests.Common;

public sealed class SeoValueTests
{
    [Theory]
    [InlineData("Endüstriyel Çözümler", "endustriyel-cozumler")]
    [InlineData("  TEST / ÜRÜN 42  ", "test-urun-42")]
    public void GenerateSlugCreatesSafeTurkishSlug(string value, string expected) =>
        Assert.Equal(expected, SeoValue.GenerateSlug(value));

    [Theory]
    [InlineData("Bad Slug")]
    [InlineData("../urun")]
    [InlineData("ürün")]
    public void SlugRejectsUnsafeCharacters(string value) =>
        Assert.Throws<ArgumentException>(() => SeoValue.Slug(value));

    [Theory]
    [InlineData("/katalog/urun")]
    [InlineData("https://www.ekiphan.com/katalog/urun")]
    public void CanonicalAcceptsSafeUrls(string value) =>
        Assert.Equal(value, SeoValue.Canonical(value));

    [Theory]
    [InlineData("http://example.com/urun")]
    [InlineData("//example.com/urun")]
    [InlineData("https://user:pass@example.com/urun")]
    public void CanonicalRejectsUnsafeUrls(string value) =>
        Assert.Throws<ArgumentException>(() => SeoValue.Canonical(value));
}
