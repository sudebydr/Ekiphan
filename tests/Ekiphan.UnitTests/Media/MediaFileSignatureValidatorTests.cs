using System.IO.Compression;
using System.Text;
using Ekiphan.Application.Media;
using Ekiphan.Domain.Media;

namespace Ekiphan.UnitTests.Media;

public sealed class MediaFileSignatureValidatorTests
{
    private readonly MediaFileSignatureValidator validator = new();

    [Theory]
    [InlineData(
        MediaAssetType.Image,
        "image/jpeg",
        "image.jpg",
        "FFD8FFE000")]
    [InlineData(
        MediaAssetType.Image,
        "image/png",
        "image.png",
        "89504E470D0A1A0A00")]
    [InlineData(
        MediaAssetType.Pdf,
        "application/pdf",
        "catalog.pdf",
        "255044462D312E37")]
    [InlineData(
        MediaAssetType.Document,
        "application/msword",
        "spec.doc",
        "D0CF11E0A1B11AE1")]
    public void KnownSignaturesAreAccepted(
        MediaAssetType assetType,
        string mimeType,
        string fileName,
        string hexContent)
    {
        using var content = new MemoryStream(
            Convert.FromHexString(hexContent));

        Assert.True(
            validator.IsValid(
                content,
                assetType,
                mimeType,
                fileName));
    }

    [Fact]
    public void WebPRequiresRiffAndWebPMarkers()
    {
        using var content = new MemoryStream(
            Encoding.ASCII.GetBytes("RIFF1234WEBP"));

        Assert.True(
            validator.IsValid(
                content,
                MediaAssetType.Image,
                "image/webp",
                "image.webp"));
    }

    [Fact]
    public void DocxRequiresExpectedPackageEntries()
    {
        using var content = new MemoryStream();
        using (var archive = new ZipArchive(
            content,
            ZipArchiveMode.Create,
            leaveOpen: true))
        {
            archive.CreateEntry("[Content_Types].xml");
            archive.CreateEntry("word/document.xml");
        }

        content.Position = 0;
        Assert.True(
            validator.IsValid(
                content,
                MediaAssetType.Document,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "spec.docx"));
    }

    [Theory]
    [InlineData("image.png", "image/jpeg")]
    [InlineData("image.jpg", "image/png")]
    [InlineData("image.exe", "image/jpeg")]
    public void MimeExtensionOrSignatureMismatchIsRejected(
        string fileName,
        string mimeType)
    {
        using var content = new MemoryStream(
            [0xFF, 0xD8, 0xFF, 0xE0]);

        Assert.False(
            validator.IsValid(
                content,
                MediaAssetType.Image,
                mimeType,
                fileName));
    }
}
