using Ekiphan.Domain.Content;

namespace Ekiphan.UnitTests.Content;

public sealed class HomepageHeroTests
{
    [Fact]
    public void PublishingRequiresTranslation()
    {
        var hero = new HomepageHero(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => hero.SetPublished(true));
    }

    [Fact]
    public void ValidTranslationCanBePublished()
    {
        var hero = new HomepageHero(Guid.NewGuid());
        hero.SetTranslation(
            "TR", "Başlık", "Alt başlık", "Kataloğa git", "/katalog",
            "Showroom", "/showroom");

        hero.SetPublished(true);

        Assert.True(hero.IsPublished);
        Assert.Equal("tr", Assert.Single(hero.Translations).LanguageCode);
    }

    [Fact]
    public void UnsafeCtaAndInvalidScheduleAreRejected()
    {
        Assert.Throws<ArgumentException>(() => new HomepageHeroTranslation(
            Guid.NewGuid(), "tr", "Başlık", null, "Git", "https://example.com", null, null));

        var hero = new HomepageHero(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentException>(
            () => hero.Configure(null, null, 0, now, now));
    }
}
