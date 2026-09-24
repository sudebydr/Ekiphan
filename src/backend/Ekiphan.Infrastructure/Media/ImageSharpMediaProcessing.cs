using System.Diagnostics;
using System.Security.Cryptography;
using Ekiphan.Application.Media;
using Ekiphan.Domain.Media;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Ekiphan.Infrastructure.Media;

internal sealed record GeneratedWebP(byte[] Content, int Width, int Height, int Quality, bool TargetExceeded);
internal sealed record GeneratedMediaVariant(MediaVariantType Type, GeneratedWebP Image);

internal interface IMediaImageDecoder
{
    Task<Image> DecodeAsync(Stream content, CancellationToken cancellationToken);
}

internal sealed class ImageSharpMediaImageDecoder : IMediaImageDecoder
{
    public async Task<Image> DecodeAsync(Stream content, CancellationToken cancellationToken)
    {
        try { return await Image.LoadAsync(content, cancellationToken); }
        catch (UnknownImageFormatException)
        {
            throw new MediaProcessingException(MediaProcessingErrorCodes.ImageCorrupted,
                "The image cannot be decoded.");
        }
    }
}

internal interface IMediaMetadataSanitizer
{
    void Sanitize(Image image, bool stripMetadata);
}

internal sealed class ImageSharpMediaMetadataSanitizer : IMediaMetadataSanitizer
{
    public void Sanitize(Image image, bool stripMetadata)
    {
        image.Mutate(x => x.AutoOrient());
        if (!stripMetadata) return;
        image.Metadata.ExifProfile = null;
        image.Metadata.XmpProfile = null;
        image.Metadata.IptcProfile = null;
        // ICC is intentionally retained to preserve color fidelity; GPS/camera data lives in EXIF.
    }
}

internal interface IMediaVariantGenerator
{
    Task<IReadOnlyList<GeneratedMediaVariant>> GenerateAsync(Image image, CancellationToken cancellationToken);
}

internal sealed class ImageSharpMediaVariantGenerator(IWebPOptimizationService optimizer) : IMediaVariantGenerator
{
    private static readonly (MediaVariantType Type, int Size)[] Sizes =
    [
        (MediaVariantType.Thumbnail, 240), (MediaVariantType.Small, 480),
        (MediaVariantType.Medium, 960), (MediaVariantType.Large, 1600),
    ];

    public async Task<IReadOnlyList<GeneratedMediaVariant>> GenerateAsync(Image image,
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
    Task<GeneratedWebP> EncodeAsync(Image image, int maxWidth, int maxHeight, CancellationToken cancellationToken);
}

internal sealed class AdaptiveWebPOptimizationService(IOptions<MediaProcessingOptions> optionsAccessor)
    : IWebPOptimizationService
{
    private readonly MediaProcessingOptions options = optionsAccessor.Value;

    public async Task<GeneratedWebP> EncodeAsync(Image source, int maxWidth, int maxHeight,
        CancellationToken cancellationToken)
    {
        using var image = source.Clone(context => context.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(Math.Min(maxWidth, source.Width), Math.Min(maxHeight, source.Height)),
            Sampler = KnownResamplers.Lanczos3,
        }));

        var target = (long)options.TargetFileSizeKb * 1024;
        var quality = options.DefaultWebPQuality;
        byte[] bytes;
        do
        {
            await using var output = new MemoryStream();
            await image.SaveAsync(output, new WebpEncoder { Quality = quality }, cancellationToken);
            bytes = output.ToArray();
            if (bytes.LongLength <= target || quality <= options.MinimumWebPQuality) break;
            quality = Math.Max(options.MinimumWebPQuality, quality - 7);
        } while (true);

        return new GeneratedWebP(bytes, image.Width, image.Height, quality, bytes.LongLength > target);
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
                if (image.Frames.Count > 1)
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
                    originalKey, command.ContentType, command.Length, hash);
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

                asset.CompleteProcessing(image.Width, image.Height, "Local", now);
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
