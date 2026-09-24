using Ekiphan.Domain.Content;

namespace Ekiphan.UnitTests.Content;

public sealed class PressReleaseTests
{
    [Fact]
    public void PublishingRequiresTranslation()
    {
        var release = new PressRelease(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => release.SetPublished(true));
    }

    [Fact]
    public void ValidTranslationCanBePublished()
    {
        var release = new PressRelease(Guid.NewGuid(), DateTimeOffset.UtcNow);

        release.SetTranslation("tr", "Başlık", "Özet", "Basın metni");
        release.SetPublished(true);

        Assert.True(release.IsPublished);
        Assert.Single(release.Translations);
    }

    [Fact]
    public void UnsupportedLanguageIsRejected()
    {
        var release = new PressRelease(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            release.SetTranslation("de", "Titel", "Zusammenfassung", null));
    }
}
