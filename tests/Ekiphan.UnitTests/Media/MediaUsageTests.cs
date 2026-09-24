using Ekiphan.Domain.Media;

namespace Ekiphan.UnitTests.Media;

public sealed class MediaUsageTests
{
    [Fact]
    public void ProductMediaUpdatePreservesDefaultRoleRule()
    {
        var usage = new ProductMedia(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ProductMediaRole.Document);

        Assert.Throws<ArgumentException>(() => usage.Update(true, 1));
    }

    [Fact]
    public void OnlyGalleryImageCanBeDefaultProductMedia()
    {
        Assert.Throws<ArgumentException>(
            () => new ProductMedia(
                Guid.NewGuid(),
                Guid.NewGuid(),
                ProductMediaRole.PdfCatalog,
                isDefault: true));
    }

    [Fact]
    public void ProductGalleryImageCanBeDefault()
    {
        var usage = new ProductMedia(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ProductMediaRole.GalleryImage,
            isDefault: true);

        Assert.True(usage.IsDefault);
    }
}
