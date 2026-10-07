using Ekiphan.Infrastructure.MediaImport;

namespace Ekiphan.UnitTests.Media;

public sealed class ProductMediaImageFormatDetectorTests
{
    public static IEnumerable<object[]> Formats =>
    [
        [new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, ".jpg", "image/jpeg"],
        [new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, ".png", "image/png"],
        [new byte[] { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50 }, ".webp", "image/webp"]
    ];

    [Theory]
    [MemberData(nameof(Formats))]
    public void DetectsSupportedImageByBinarySignature(byte[] content, string extension, string mime)
    {
        var detected = ProductMediaImageFormatDetector.TryDetect(content, out var format);

        Assert.True(detected);
        Assert.Equal(extension, format.Extension);
        Assert.Equal(mime, format.ContentType);
    }

    [Fact]
    public void JfifExtensionIsAcceptedForJpegButWrongKnownExtensionIsNot()
    {
        var jpeg = new ProductMediaImageFormat(".jpg", "image/jpeg");

        Assert.True(ProductMediaImageFormatDetector.MatchesDeclaredExtension(".jfif", jpeg));
        Assert.False(ProductMediaImageFormatDetector.MatchesDeclaredExtension(".png", jpeg));
        Assert.False(ProductMediaImageFormatDetector.IsDeclaredImageExtension(".PP"));
    }

    [Fact]
    public void UnsupportedBinaryIsNotDetectedAsAnImage()
    {
        Assert.False(ProductMediaImageFormatDetector.TryDetect([0x52, 0x41, 0x52, 0x21], out _));
    }
}
