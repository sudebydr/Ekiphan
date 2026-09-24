using Ekiphan.Domain.Media;
using FluentValidation;

namespace Ekiphan.Application.Media;

public enum MediaProcessingMode { Auto = 1, Synchronous = 2, Asynchronous = 3 }

public sealed record MediaUploadCommand(Stream Content, string FileName, string ContentType,
    long Length, string LanguageCode, string Title, string AltText, string? Description,
    MediaProcessingMode ProcessingMode, Guid? UserId = null);

public sealed record MediaVariantDto(MediaVariantType VariantType, int Width, int Height,
    long FileSize, string MimeType, string Url, int Quality);
public sealed record MediaProcessingWarningDto(string Code, string Message);
public sealed record MediaUploadResultDto(Guid MediaAssetId, Guid? DuplicateOfMediaAssetId,
    MediaProcessingStatus ProcessingStatus, string OriginalFileName, string ContentHash,
    int? OriginalWidth, int? OriginalHeight, IReadOnlyList<MediaVariantDto> Variants,
    IReadOnlyList<MediaProcessingWarningDto> Warnings);
public sealed record MediaProcessingStatusDto(Guid MediaAssetId, MediaProcessingStatus Status,
    string? ErrorCode, string? ErrorMessage, IReadOnlyList<MediaVariantDto> Variants);
public sealed record RetryMediaProcessingCommand(Guid MediaAssetId);
public sealed record ArchiveMediaCommand(Guid MediaAssetId);

public sealed class MediaProcessingOptions
{
    public const string SectionName = "MediaProcessing";
    public int MaxUploadSizeMb { get; set; } = 25;
    public int MaxWidth { get; set; } = 12000;
    public int MaxHeight { get; set; } = 12000;
    public long MaxPixelCount { get; set; } = 60_000_000;
    public int DefaultWebPQuality { get; set; } = 82;
    public int MinimumWebPQuality { get; set; } = 55;
    public int TargetFileSizeKb { get; set; } = 200;
    public bool PreserveOriginal { get; set; } = true;
    public bool StripMetadata { get; set; } = true;
    public bool EnableDuplicateDetection { get; set; } = true;
    public bool EnableVirusScanning { get; set; } = true;
    public int ProcessingTimeoutSeconds { get; set; } = 60;
    public int MaxConcurrentJobs { get; set; } = 4;
}

public static class MediaProcessingErrorCodes
{
    public const string FileRequired = "MEDIA_FILE_REQUIRED";
    public const string FileEmpty = "MEDIA_FILE_EMPTY";
    public const string FileTooLarge = "MEDIA_FILE_TOO_LARGE";
    public const string ExtensionNotSupported = "MEDIA_EXTENSION_NOT_SUPPORTED";
    public const string SignatureInvalid = "MEDIA_SIGNATURE_INVALID";
    public const string MimeTypeInvalid = "MEDIA_MIME_TYPE_INVALID";
    public const string ImageCorrupted = "MEDIA_IMAGE_CORRUPTED";
    public const string DimensionsInvalid = "MEDIA_IMAGE_DIMENSIONS_INVALID";
    public const string PixelLimitExceeded = "MEDIA_PIXEL_LIMIT_EXCEEDED";
    public const string ThreatDetected = "MEDIA_THREAT_DETECTED";
    public const string ScannerUnavailable = "MEDIA_SCANNER_UNAVAILABLE";
    public const string ProcessingFailed = "MEDIA_PROCESSING_FAILED";
    public const string StorageFailed = "MEDIA_STORAGE_FAILED";
    public const string NotFound = "MEDIA_NOT_FOUND";
    public const string RetryNotAllowed = "MEDIA_RETRY_NOT_ALLOWED";
    public const string Archived = "MEDIA_ARCHIVED";
}

public sealed class MediaProcessingException(string code, string message, int statusCode = 422)
    : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}

public interface IMediaProcessingService
{
    Task<MediaUploadResultDto> ProcessAsync(MediaUploadCommand command, CancellationToken cancellationToken = default);
    Task<MediaProcessingStatusDto?> GetStatusAsync(Guid mediaAssetId, CancellationToken cancellationToken = default);
    Task<MediaProcessingStatusDto> RetryAsync(Guid mediaAssetId, CancellationToken cancellationToken = default);
    Task<bool> ArchiveAsync(Guid mediaAssetId, CancellationToken cancellationToken = default);
}

public interface IMediaProcessingRepository
{
    Task<MediaAsset?> FindReusableByHashAsync(string hash, CancellationToken cancellationToken);
    Task<MediaAsset?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(MediaAsset asset, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
}

public sealed class MediaUploadCommandValidator : AbstractValidator<MediaUploadCommand>
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
    public MediaUploadCommandValidator(MediaProcessingOptions options)
    {
        RuleFor(x => x.Content).NotNull();
        RuleFor(x => x.Length).GreaterThan(0).LessThanOrEqualTo((long)options.MaxUploadSizeMb * 1024 * 1024);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(260)
            .Must(x => x == Path.GetFileName(x) && Extensions.Contains(Path.GetExtension(x)));
        RuleFor(x => x.ContentType).Must(x => x is "image/jpeg" or "image/png" or "image/webp");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(250);
        RuleFor(x => x.AltText).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ProcessingMode).IsInEnum();
    }
}
