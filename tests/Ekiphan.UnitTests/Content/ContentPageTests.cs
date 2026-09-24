using Ekiphan.Domain.Content;

namespace Ekiphan.UnitTests.Content;

public sealed class ContentPageTests
{
    [Fact]
    public void PageCanBePublishedAfterTranslationIsAdded()
    {
        var page = new ContentPage(Guid.NewGuid(), " about_us ");
        var now = DateTimeOffset.UtcNow;
        page.SetTranslation(
            "TR",
            "Hakkımızda",
            "hakkimizda",
            "Özet",
            "Kurumsal içerik",
            null,
            null,
            "/sayfa/hakkimizda",
            false,
            false);

        page.SetStatus(ContentStatus.Published, now);

        Assert.Equal("ABOUT_US", page.Code);
        Assert.Equal(ContentStatus.Published, page.Status);
        Assert.Equal(now, page.PublishedAt);
        Assert.Equal("tr", Assert.Single(page.Translations).LanguageCode);
    }

    [Fact]
    public void EmptyPageCannotBePublished()
    {
        var page = new ContentPage(Guid.NewGuid(), "ABOUT");

        Assert.Throws<InvalidOperationException>(
            () => page.SetStatus(
                ContentStatus.Published,
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void UnsafeCanonicalUrlIsRejected()
    {
        var page = new ContentPage(Guid.NewGuid(), "ABOUT");

        Assert.Throws<ArgumentException>(
            () => page.SetTranslation(
                "tr",
                "Hakkımızda",
                "hakkimizda",
                null,
                "İçerik",
                null,
                null,
                "http://example.com/about",
                false,
                false));
    }
}

