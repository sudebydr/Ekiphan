using System.Diagnostics;
using System.Security.Cryptography;
using Ekiphan.Application.Media;
using Ekiphan.Domain.Media;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace Ekiphan.Infrastructure.Media;

internal sealed record GeneratedWebP(byte[] Content, int Width, int Height, int Quality, bool TargetExceeded);
internal sealed record GeneratedMediaVariant(MediaVariantType Type, GeneratedWebP Image);

internal sealed class DecodedMediaImage(SKBitmap bitmap, SKEncodedOrigin origin, int frameCount) : IDisposable
{
    public SKBitmap Bitmap { get; private set; } = bitmap;
    public int Width => Bitmap.Width;
    public int Height => Bitmap.Height;
    public int FrameCount { get; } = frameCount;

    public void AutoOrient()
    {
        if (origin == SKEncodedOrigin.TopLeft) return;
        var swapped = (int)origin >= (int)SKEncodedOrigin.LeftTop;
        var oriented = new SKBitmap(swapped ? Height : Width, swapped ? Width : Height);
        using var canvas = new SKCanvas(oriented);
        switch (origin)
        {
            case SKEncodedOrigin.TopRight: canvas.Translate(Width, 0); canvas.Scale(-1, 1); break;
            case SKEncodedOrigin.BottomRight: canvas.Translate(Width, Height); canvas.RotateDegrees(180); break;
            case SKEncodedOrigin.BottomLeft: canvas.Translate(0, Height); canvas.Scale(1, -1); break;
            case SKEncodedOrigin.LeftTop: canvas.RotateDegrees(90); canvas.Scale(1, -1); break;
            case SKEncodedOrigin.RightTop: canvas.Translate(Height, 0); canvas.RotateDegrees(90); break;
            case SKEncodedOrigin.RightBottom: canvas.Translate(Height, Width); canvas.RotateDegrees(90); canvas.Scale(-1, 1); break;
            case SKEncodedOrigin.LeftBottom: canvas.Translate(0, Width); canvas.RotateDegrees(270); break;
        }
        canvas.DrawBitmap(Bitmap, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest));
        Bitmap.Dispose();
        Bitmap = oriented;
    }

    public void Dispose() => Bitmap.Dispose();
}

internal interface IMediaImageDecoder
{
    Task<DecodedMediaImage> DecodeAsync(Stream content, CancellationToken cancellationToken);
}

internal sealed class SkiaMediaImageDecoder(IOptions<MediaProcessingOptions> optionsAccessor) : IMediaImageDecoder
{
    public Task<DecodedMediaImage> DecodeAsync(Stream content, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var managedStream = new SKManagedStream(content, false);
        using var codec = SKCodec.Create(managedStream);
        if (codec is null)
            throw new MediaProcessingException(MediaProcessingErrorCodes.ImageCorrupted,
                "The image cannot be decoded.");
        var options = optionsAccessor.Value;
        if (codec.FrameCount > 1)
            throw new MediaProcessingException(MediaProcessingErrorCodes.ImageCorrupted, "Animated images are not accepted.");
        if (codec.Info.Width <= 0 || codec.Info.Height <= 0 || codec.Info.Width > options.MaxWidth || codec.Info.Height > options.MaxHeight)
            throw new MediaProcessingException(MediaProcessingErrorCodes.DimensionsInvalid, "Image dimensions exceed the configured limit.");
        if ((long)codec.Info.Width * codec.Info.Height > options.MaxPixelCount)
            throw new MediaProcessingException(MediaProcessingErrorCodes.PixelLimitExceeded, "Image pixel count exceeds the configured limit.");
        var bitmap = SKBitmap.Decode(codec);
        if (bitmap is null)
            throw new MediaProcessingException(MediaProcessingErrorCodes.ImageCorrupted,
                "The image cannot be decoded.");
        return Task.FromResult(new DecodedMediaImage(bitmap, codec.EncodedOrigin, codec.FrameCount));
    }
}

internal interface IMediaMetadataSanitizer
{
    void Sanitize(DecodedMediaImage image, bool stripMetadata);
}

internal sealed class SkiaMediaMetadataSanitizer : IMediaMetadataSanitizer
{
    public void Sanitize(DecodedMediaImage image, bool stripMetadata)
    {
        image.AutoOrient();
        // Pixel encoding never copies EXIF/XMP/IPTC; the original upload remains unchanged.
    }
}

internal interface IMediaVariantGenerator
{
    Task<IReadOnlyList<GeneratedMediaVariant>> GenerateAsync(DecodedMediaImage image, CancellationToken cancellationToken);
}

internal sealed class SkiaMediaVariantGenerator(IWebPOptimizationService optimizer) : IMediaVariantGenerator
{
    private static readonly (MediaVariantType Type, int Size)[] Sizes =
    [
        (MediaVariantType.Thumbnail, 240), (MediaVariantType.Small, 480),
        (MediaVariantType.Medium, 960), (MediaVariantType.Large, 1600),
    ];

    public async Task<IReadOnlyList<GeneratedMediaVariant>> GenerateAsync(DecodedMediaImage image,
        CancellationToken cancellationToken)
    {
        var result = new List<GeneratedMediaVariant>(Sizes.Length);
        foreach (var (type, size) in Sizes)
            result.Add(new(type, await optimizer.EncodeAsync(image, size, size, cancellationToken)));
        return result;
    }
}

internal interface IWebPOptimizationService
{
    Task<GeneratedWebP> EncodeAsync(DecodedMediaImage image, int maxWidth, int maxHeight, CancellationToken cancellationToken);
}

internal sealed class AdaptiveWebPOptimizationService(IOptions<MediaProcessingOptions> optionsAccessor)
    : IWebPOptimizationService
{
    private readonly MediaProcessingOptions options = optionsAccessor.Value;

    public Task<GeneratedWebP> EncodeAsync(DecodedMediaImage source, int maxWidth, int maxHeight,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ratio = Math.Min(1d, Math.Min((double)maxWidth / source.Width, (double)maxHeight / source.Height));
        var width = Math.Max(1, (int)Math.Round(source.Width * ratio));
        var height = Math.Max(1, (int)Math.Round(source.Height * ratio));
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
            canvas.DrawBitmap(source.Bitmap, new SKRect(0, 0, width, height), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        using var image = SKImage.FromBitmap(bitmap);

        var target = (long)options.TargetFileSizeKb * 1024;
        var quality = options.DefaultWebPQuality;
        byte[] bytes;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var output = image.Encode(SKEncodedImageFormat.Webp, quality)
                ?? throw new MediaProcessingException(MediaProcessingErrorCodes.ProcessingFailed, "WebP encoding failed.");
            bytes = output.ToArray();
            if (bytes.LongLength <= target || quality <= options.MinimumWebPQuality) break;
            quality = Math.Max(options.MinimumWebPQuality, quality - 7);
        } while (true);

        return Task.FromResult(new GeneratedWebP(bytes, width, height, quality, bytes.LongLength > target));
    }
}

internal sealed class MediaProcessingService(
    IValidator<MediaUploadCommand> validator,
    IMediaFileSignatureValidator signatureValidator,
    IMediaThreatScanner threatScanner,
    IMediaFileStorage storage,
    IMediaProcessingRepository repository,
    IMediaImageDecoder decoder,
    IMediaMetadataSanitizer metadataSanitizer,
    IMediaVariantGenerator variantGenerator,
    IOptions<MediaProcessingOptions> optionsAccessor,
    TimeProvider clock,
    ILogger<MediaProcessingService> logger) : IMediaProcessingService, IDisposable
{
    private static readonly Action<ILogger, string, long, Exception?> UploadStarted =
        LoggerMessage.Define<string, long>(LogLevel.Information, new EventId(5101, "MediaUploadStarted"),
            "Media upload started {FileName} {FileSize}");
    private static readonly Action<ILogger, Guid, string, Exception?> DuplicateReused =
        LoggerMessage.Define<Guid, string>(LogLevel.Information, new EventId(5102, "MediaDuplicateReused"),
            "Duplicate media reused {MediaAssetId} {ContentHash}");
    private static readonly Action<ILogger, Guid, string, long, Exception?> ProcessingCompleted =
        LoggerMessage.Define<Guid, string, long>(LogLevel.Information, new EventId(5103, "MediaProcessingCompleted"),
            "Media processing completed {MediaAssetId} {ContentHash} {ProcessingDurationMs}");
    private static readonly Action<ILogger, string, Exception?> CompensationFailed =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(5104, "MediaCompensationFailed"),
            "Media compensation failed {StorageKey}");
    private static readonly Action<ILogger, string, long, Exception?> ProcessingFailed =
        LoggerMessage.Define<string, long>(LogLevel.Error, new EventId(5105, "MediaProcessingFailed"),
            "Media processing failed {FileName} {ProcessingDurationMs}");
    private readonly MediaProcessingOptions options = optionsAccessor.Value;
    private readonly SemaphoreSlim concurrency = new(Math.Max(1, optionsAccessor.Value.MaxConcurrentJobs));

    public async Task<MediaUploadResultDto> ProcessAsync(MediaUploadCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            throw new MediaProcessingException(MediaProcessingErrorCodes.FileRequired,
                validation.Errors[0].ErrorMessage, 400);

        if (command.ProcessingMode == MediaProcessingMode.Asynchronous)
            throw new MediaProcessingException(MediaProcessingErrorCodes.ProcessingFailed,
                "Asynchronous processing requires a configured durable job provider.", 503);

        await concurrency.WaitAsync(cancellationToken);
        var stopwatch = Stopwatch.StartNew();
        var savedKeys = new List<string>();
        try
        {
            UploadStarted(logger, command.FileName, command.Length, null);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(options.ProcessingTimeoutSeconds));
            var token = timeout.Token;

            if (!command.Content.CanSeek) throw new MediaProcessingException(
                MediaProcessingErrorCodes.ImageCorrupted, "The upload stream must be seekable.");
            command.Content.Position = 0;
            if (!signatureValidator.IsValid(command.Content, MediaAssetType.Image, command.ContentType, command.FileName))
                throw new MediaProcessingException(MediaProcessingErrorCodes.SignatureInvalid,
                    "File signature, MIME type and extension do not match.");

            command.Content.Position = 0;
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(command.Content, token));
            if (options.EnableDuplicateDetection)
            {
                var duplicate = await repository.FindReusableByHashAsync(hash, token);
                if (duplicate is not null)
                {
                    DuplicateReused(logger, duplicate.Id, hash, null);
                    return ToUploadResult(duplicate, duplicate.Id, []);
                }
            }

            if (options.EnableVirusScanning)
            {
                command.Content.Position = 0;
                var scan = await threatScanner.ScanAsync(command.Content, command.FileName, command.ContentType, token);
                if (scan == MediaThreatScanStatus.Unavailable)
                    throw new MediaProcessingException(MediaProcessingErrorCodes.ScannerUnavailable,
                        "Media threat scanning is unavailable.", 503);
                if (scan == MediaThreatScanStatus.ThreatFound)
                    throw new MediaProcessingException(MediaProcessingErrorCodes.ThreatDetected,
                        "The uploaded file was rejected by threat scanning.");
            }

            command.Content.Position = 0;
            var image = await decoder.DecodeAsync(command.Content, token);
            using (image)
            {
                if (image.FrameCount > 1)
                    throw new MediaProcessingException(MediaProcessingErrorCodes.ImageCorrupted,
                        "Animated images are not accepted.");
                if (image.Width <= 0 || image.Height <= 0 || image.Width > options.MaxWidth || image.Height > options.MaxHeight)
                    throw new MediaProcessingException(MediaProcessingErrorCodes.DimensionsInvalid,
                        "Image dimensions exceed the configured limit.");
                if ((long)image.Width * image.Height > options.MaxPixelCount)
                    throw new MediaProcessingException(MediaProcessingErrorCodes.PixelLimitExceeded,
                        "Image pixel count exceeds the configured limit.");

                metadataSanitizer.Sanitize(image, options.StripMetadata);

                var now = clock.GetUtcNow();
                var id = Guid.NewGuid();
                var extension = Path.GetExtension(command.FileName).ToLowerInvariant();
                var originalKey = $"media/{now:yyyy/MM}/{id:N}/original/{Guid.NewGuid():N}{extension}";
                var asset = MediaAsset.CreateFile(id, MediaAssetType.Image, command.FileName,
                    originalKey, command.ContentType, command.Length, hash, storage.ProviderName);
                asset.AddTranslation(command.LanguageCode, command.Title, command.AltText, command.Description);
                asset.BeginProcessing(command.UserId);

                if (options.PreserveOriginal)
                {
                    command.Content.Position = 0;
                    await storage.SaveAsync(originalKey, command.Content, token);
                    savedKeys.Add(originalKey);
                }

                var warnings = new List<MediaProcessingWarningDto>();
                foreach (var generatedVariant in await variantGenerator.GenerateAsync(image, token))
                {
                    var type = generatedVariant.Type;
                    var generated = generatedVariant.Image;
                    var key = $"media/{now:yyyy/MM}/{id:N}/{type.ToString().ToLowerInvariant()}/{Guid.NewGuid():N}.webp";
                    await using var variantStream = new MemoryStream(generated.Content, writable: false);
                    await storage.SaveAsync(key, variantStream, token);
                    savedKeys.Add(key);
                    asset.AddVariant(MediaVariant.Create(id, type, generated.Width, generated.Height,
                        generated.Content.LongLength, generated.Quality, key));
                    if (generated.TargetExceeded)
                        warnings.Add(new("MEDIA_TARGET_SIZE_NOT_REACHED",
                            $"{type} variant is valid but exceeds the configured target size."));
                }

                asset.CompleteProcessing(image.Width, image.Height, storage.ProviderName, now);
                await repository.AddAsync(asset, token);
                await repository.SaveAsync(token);
                ProcessingCompleted(logger, id, hash, stopwatch.ElapsedMilliseconds, null);
                return ToUploadResult(asset, null, warnings);
            }
        }
        catch
        {
            foreach (var key in savedKeys)
            {
                try { await storage.DeleteAsync(key, CancellationToken.None); }
                catch (Exception cleanupError) { CompensationFailed(logger, key, cleanupError); }
            }
            ProcessingFailed(logger, command.FileName, stopwatch.ElapsedMilliseconds, null);
            throw;
        }
        finally { concurrency.Release(); }
    }

    public async Task<MediaProcessingStatusDto?> GetStatusAsync(Guid mediaAssetId, CancellationToken cancellationToken = default)
    {
        var asset = await repository.FindAsync(mediaAssetId, cancellationToken);
        return asset is null ? null : ToStatus(asset);
    }

    public async Task<MediaProcessingStatusDto> RetryAsync(Guid mediaAssetId, CancellationToken cancellationToken = default)
    {
        var asset = await repository.FindAsync(mediaAssetId, cancellationToken)
            ?? throw new MediaProcessingException(MediaProcessingErrorCodes.NotFound, "Media asset was not found.", 404);
        if (asset.ProcessingStatus is not (MediaProcessingStatus.Failed or MediaProcessingStatus.CompensationRequired))
            throw new MediaProcessingException(MediaProcessingErrorCodes.RetryNotAllowed,
                "Only failed media processing can be retried.", 409);
        throw new MediaProcessingException(MediaProcessingErrorCodes.RetryNotAllowed,
            "The original upload stream is not retained for retry. Upload the source again.", 409);
    }

    public async Task<bool> ArchiveAsync(Guid mediaAssetId, CancellationToken cancellationToken = default)
    {
        var asset = await repository.FindAsync(mediaAssetId, cancellationToken);
        if (asset is null) return false;
        if (asset.Status != MediaStatus.Archived)
        {
            asset.Archive(clock.GetUtcNow());
            await repository.SaveAsync(cancellationToken);
        }
        return true;
    }

    private MediaUploadResultDto ToUploadResult(MediaAsset asset, Guid? duplicateId,
        IReadOnlyList<MediaProcessingWarningDto> warnings) => new(asset.Id, duplicateId,
        asset.ProcessingStatus, asset.OriginalFileName!, asset.Sha256Checksum!, asset.OriginalWidth,
        asset.OriginalHeight, asset.Variants.Select(ToVariant).ToArray(), warnings);

    private MediaProcessingStatusDto ToStatus(MediaAsset asset) => new(asset.Id, asset.ProcessingStatus,
        asset.ProcessingErrorCode, asset.ProcessingErrorMessage, asset.Variants.Select(ToVariant).ToArray());

    private MediaVariantDto ToVariant(MediaVariant variant) => new(variant.VariantType, variant.Width,
        variant.Height, variant.FileSizeBytes, variant.MimeType, storage.GetPublicUrl(variant.StorageKey), variant.Quality);

    public void Dispose() => concurrency.Dispose();
}
