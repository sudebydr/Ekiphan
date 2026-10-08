using Ekiphan.Application.Media;
using Ekiphan.Infrastructure.Media;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace Ekiphan.UnitTests.Media;

public sealed class SkiaMediaProcessingTests
{
    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg)]
    [InlineData(SKEncodedImageFormat.Png)]
    [InlineData(SKEncodedImageFormat.Webp)]
    public async Task DecodeAndEncodePreservesSizeAndLeavesUploadStreamOpen(SKEncodedImageFormat format)
    {
        using var source = new SKBitmap(24, 48);
        source.Erase(SKColors.Red);
        using var encoded = source.Encode(format, 90);
        using var stream = new MemoryStream(encoded.ToArray());
        using var image = await new SkiaMediaImageDecoder(Options.Create(new MediaProcessingOptions()))
            .DecodeAsync(stream, CancellationToken.None);
        new SkiaMediaMetadataSanitizer().Sanitize(image, true);
        var result = await new AdaptiveWebPOptimizationService(Options.Create(new MediaProcessingOptions()))
            .EncodeAsync(image, 960, 960, CancellationToken.None);
        using var output = SKBitmap.Decode(result.Content);
        Assert.Equal(24, output.Width);
        Assert.Equal(48, output.Height);
        Assert.True(stream.CanRead);
        stream.Position = 0;
        Assert.NotEqual(-1, stream.ReadByte());
    }

    [Fact]
    public void OrientationRotatesWithoutCropping()
    {
        using var image = new DecodedMediaImage(new SKBitmap(2, 3), SKEncodedOrigin.RightTop, 1);
        image.Bitmap.SetPixel(0, 0, SKColors.Red);
        image.AutoOrient();
        Assert.Equal(3, image.Width);
        Assert.Equal(2, image.Height);
        Assert.Equal(SKColors.Red, image.Bitmap.GetPixel(2, 0));
    }

    [Fact]
    public async Task InvalidBinaryIsRejected()
    {
        using var stream = new MemoryStream([1, 2, 3, 4]);
        await Assert.ThrowsAsync<MediaProcessingException>(() =>
            new SkiaMediaImageDecoder(Options.Create(new MediaProcessingOptions()))
                .DecodeAsync(stream, CancellationToken.None));
    }

    [Fact]
    public async Task WebpPreservesTransparency()
    {
        using var bitmap = new SKBitmap(10, 10);
        bitmap.Erase(SKColors.Transparent);
        bitmap.SetPixel(5, 5, SKColors.Red);
        using var source = new DecodedMediaImage(bitmap.Copy(), SKEncodedOrigin.TopLeft, 1);
        var result = await new AdaptiveWebPOptimizationService(Options.Create(new MediaProcessingOptions()))
            .EncodeAsync(source, 960, 960, CancellationToken.None);
        using var output = SKBitmap.Decode(result.Content);
        Assert.Equal(0, output.GetPixel(0, 0).Alpha);
        Assert.Equal(255, output.GetPixel(5, 5).Alpha);
    }
}
