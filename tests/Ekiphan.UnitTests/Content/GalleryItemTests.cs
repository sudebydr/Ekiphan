using Ekiphan.Domain.Content;

namespace Ekiphan.UnitTests.Content;

public sealed class GalleryItemTests
{
    [Fact]
    public void PublishedItemRequiresTranslation()
    {
        var item = new GalleryItem(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => item.SetPublished(true));
    }

    [Fact]
    public void ValidTranslationAllowsPublishing()
    {
        var item = new GalleryItem(Guid.NewGuid(), Guid.NewGuid());

        item.SetTranslation("tr", "Showroom", "Yeni showroom alanımız.");
        item.SetPublished(true);

        Assert.True(item.IsPublished);
        Assert.Single(item.Translations);
    }

    [Fact]
    public void EmptyMediaIdentifierIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new GalleryItem(Guid.NewGuid(), Guid.Empty));
    }
}
