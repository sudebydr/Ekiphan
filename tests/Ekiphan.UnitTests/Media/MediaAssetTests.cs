using Ekiphan.Domain.Media;

namespace Ekiphan.UnitTests.Media;

public sealed class MediaAssetTests
{
    private const string Checksum =
        "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";

    [Fact]
    public void FileNameCannotContainAPath()
    {
        Assert.Throws<ArgumentException>(
            () => MediaAsset.CreateFile(
                Guid.NewGuid(),
                MediaAssetType.Image,
                "../product.webp",
                "products/product.webp",
                "image/webp",
                100,
                Checksum));
    }

    [Fact]
    public void StorageKeyRejectsTraversal()
    {
        Assert.Throws<ArgumentException>(
            () => MediaAsset.CreateFile(
                Guid.NewGuid(),
                MediaAssetType.Image,
                "product.webp",
                "products/../product.webp",
                "image/webp",
                100,
                Checksum));
    }

    [Fact]
    public void MimeTypeMustMatchFileExtension()
    {
        Assert.Throws<ArgumentException>(
            () => MediaAsset.CreateFile(
                Guid.NewGuid(),
                MediaAssetType.Image,
                "product.jpg",
                "products/product.jpg",
                "image/png",
                100,
                Checksum));
    }

    [Fact]
    public void ImageCannotExceedConfiguredLimit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MediaAsset.CreateFile(
                Guid.NewGuid(),
                MediaAssetType.Image,
                "product.webp",
                "products/product.webp",
                "image/webp",
                15L * 1024 * 1024 + 1,
                Checksum));
    }

    [Fact]
    public void ExternalVideoMustUseHttps()
    {
        Assert.Throws<ArgumentException>(
            () => MediaAsset.CreateExternalVideo(
                Guid.NewGuid(),
                "http://video.example.com/tour"));
    }

    [Fact]
    public void ImageTranslationRequiresAltText()
    {
        var asset = CreateImage();

        Assert.Throws<ArgumentException>(
            () => asset.AddTranslation("tr", "Ürün görseli"));
    }

    [Fact]
    public void ArchivingPreservesAssetAndRecordsTimestamp()
    {
        var asset = CreateImage();
        var archivedAt = DateTimeOffset.UtcNow;

        asset.Archive(archivedAt);

        Assert.Equal(MediaStatus.Archived, asset.Status);
        Assert.Equal(archivedAt, asset.ArchivedAt);
    }

    [Fact]
    public void ExistingTranslationCanBeUpdated()
    {
        var asset = CreateImage();
        asset.AddTranslation(
            "tr",
            "Eski başlık",
            "Eski alt metin",
            "Eski açıklama");

        asset.SetTranslation(
            "TR",
            "Yeni başlık",
            "Yeni alt metin",
            "Yeni açıklama");

        var translation = Assert.Single(asset.Translations);
        Assert.Equal("Yeni başlık", translation.Title);
        Assert.Equal("Yeni alt metin", translation.AltText);
        Assert.Equal("Yeni açıklama", translation.Description);
    }

    [Fact]
    public void TranslationDescriptionHasAControlledMaximumLength()
    {
        var asset = CreateImage();

        Assert.Throws<ArgumentException>(
            () => asset.AddTranslation(
                "tr",
                "Başlık",
                "Alt metin",
                new string('a', 2001)));
    }

    private static MediaAsset CreateImage() =>
        MediaAsset.CreateFile(
            Guid.NewGuid(),
            MediaAssetType.Image,
            "product.webp",
            "products/product.webp",
            "image/webp",
            100,
            Checksum);
}
