using Ekiphan.Domain.Media;

namespace Ekiphan.Application.Media;

public static class MediaUploadLimits
{
    public const long MaximumRequestBytes = 50L * 1024 * 1024 + 64 * 1024;
}

public sealed record UploadMediaCommand(
    Stream Content,
    string FileName,
    string ContentType,
    long Length,
    MediaAssetType AssetType,
    string LanguageCode,
    string Title,
    string? AltText,
    string? Description = null);

public sealed record UploadedMediaResult(
    Guid Id,
    MediaAssetType AssetType,
    string OriginalFileName,
    string StorageKey,
    string MimeType,
    long FileSizeBytes,
    string Sha256Checksum);

public enum MediaThreatScanStatus
{
    Clean = 1,
    ThreatFound = 2,
    Unavailable = 3,
}

public interface IMediaThreatScanner
{
    Task<MediaThreatScanStatus> ScanAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}

public interface IMediaFileStorage
{
    bool IsConfigured { get; }

    Task SaveAsync(
        string storageKey,
        Stream content,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream?>(null);

    Task MoveAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    string GetPublicUrl(string storageKey) => storageKey;
}

public interface IMediaAssetRepository
{
    Task AddAsync(
        MediaAsset asset,
        CancellationToken cancellationToken = default);
}

public interface IMediaFileSignatureValidator
{
    bool IsValid(
        Stream content,
        MediaAssetType assetType,
        string contentType,
        string fileName);
}

public sealed class MediaUploadUnavailableException(string message)
    : Exception(message);

public sealed class UnsafeMediaFileException(string message)
    : Exception(message);

public sealed class DuplicateMediaContentException : Exception
{
    public DuplicateMediaContentException()
        : base("A media asset with the same content already exists.")
    {
    }
}
