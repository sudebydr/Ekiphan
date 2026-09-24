using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Media;

public sealed class MediaAsset : Entity
{
    private readonly List<MediaAssetTranslation> _translations = [];
    private readonly List<MediaVariant> _variants = [];

    private MediaAsset()
    {
    }

    private MediaAsset(Guid id, MediaAssetType assetType)
        : base(id)
    {
        AssetType = assetType;
    }

    public MediaAssetType AssetType { get; private set; }

    public MediaStatus Status { get; private set; } = MediaStatus.Active;

    public string? OriginalFileName { get; private set; }

    public string? StorageKey { get; private set; }

    public string? MimeType { get; private set; }

    public long? FileSizeBytes { get; private set; }

    public string? Sha256Checksum { get; private set; }

    public string? ExternalUrl { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public string? OriginalExtension { get; private set; }
    public int? OriginalWidth { get; private set; }
    public int? OriginalHeight { get; private set; }
    public string StorageProvider { get; private set; } = "Local";
    public MediaProcessingStatus ProcessingStatus { get; private set; } = MediaProcessingStatus.Completed;
    public string? ProcessingErrorCode { get; private set; }
    public string? ProcessingErrorMessage { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<MediaAssetTranslation> Translations => _translations;
    public IReadOnlyCollection<MediaVariant> Variants => _variants;

    public static MediaAsset CreateFile(
        Guid id,
        MediaAssetType assetType,
        string originalFileName,
        string storageKey,
        string mimeType,
        long fileSizeBytes,
        string sha256Checksum)
    {
        if (assetType == MediaAssetType.ExternalVideo)
        {
            throw new ArgumentException(
                "External video assets must be created from a URL.",
                nameof(assetType));
        }

        var safeOriginalName = Path.GetFileName(originalFileName);

        if (!string.Equals(
                originalFileName,
                safeOriginalName,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Original file name cannot contain a path.",
                nameof(originalFileName));
        }

        var normalizedMime = MediaGuard.Required(mimeType, 150, nameof(mimeType))
            .ToLowerInvariant();
        MediaGuard.ValidateMimeAndExtension(
            assetType,
            normalizedMime,
            safeOriginalName);

        var maximumSize = assetType == MediaAssetType.Image
            ? 25L * 1024 * 1024
            : 50L * 1024 * 1024;

        if (fileSizeBytes <= 0 || fileSizeBytes > maximumSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fileSizeBytes),
                $"File size must be between 1 and {maximumSize} bytes.");
        }

        var normalizedChecksum = MediaGuard.Required(
                sha256Checksum,
                64,
                nameof(sha256Checksum))
            .ToUpperInvariant();

        if (normalizedChecksum.Length != 64 ||
            normalizedChecksum.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException(
                "Checksum must be a 64-character SHA-256 hex value.",
                nameof(sha256Checksum));
        }

        return new MediaAsset(id, assetType)
        {
            OriginalFileName = safeOriginalName,
            StorageKey = MediaGuard.StorageKey(storageKey),
            MimeType = normalizedMime,
            FileSizeBytes = fileSizeBytes,
            Sha256Checksum = normalizedChecksum,
        };
    }

    public static MediaAsset CreateExternalVideo(Guid id, string externalUrl)
    {
        if (!Uri.TryCreate(externalUrl?.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException(
                "External video URL must be an absolute HTTPS URL.",
                nameof(externalUrl));
        }

        return new MediaAsset(id, MediaAssetType.ExternalVideo)
        {
            ExternalUrl = uri.AbsoluteUri,
        };
    }

    public void AddTranslation(
        string languageCode,
        string title,
        string? altText = null,
        string? description = null)
    {
        var normalizedLanguage = MediaGuard.Required(
                languageCode,
                2,
                nameof(languageCode))
            .ToLowerInvariant();

        if (!LanguageCodes.IsSupported(normalizedLanguage))
        {
            throw new ArgumentOutOfRangeException(
                nameof(languageCode),
                languageCode,
                "Only Turkish and English are supported.");
        }

        if (_translations.Any(item => item.LanguageCode == normalizedLanguage))
        {
            throw new InvalidOperationException(
                $"Translation '{normalizedLanguage}' already exists.");
        }

        if (AssetType == MediaAssetType.Image && string.IsNullOrWhiteSpace(altText))
        {
            throw new ArgumentException(
                "Image translations require alt text.",
                nameof(altText));
        }

        _translations.Add(
            new MediaAssetTranslation(
                Id,
                normalizedLanguage,
                title,
                altText,
                description));
    }

    public void SetTranslation(
        string languageCode,
        string title,
        string? altText = null,
        string? description = null)
    {
        var language = MediaGuard.Required(
                languageCode,
                2,
                nameof(languageCode))
            .ToLowerInvariant();
        var translation = _translations.SingleOrDefault(
            item => item.LanguageCode == language);
        if (translation is null)
        {
            AddTranslation(language, title, altText, description);
            return;
        }

        if (AssetType == MediaAssetType.Image &&
            string.IsNullOrWhiteSpace(altText))
        {
            throw new ArgumentException(
                "Image translations require alt text.",
                nameof(altText));
        }

        translation.Update(title, altText, description);
    }

    public void RemoveTranslation(string languageCode)
    {
        var language = MediaGuard.Required(
                languageCode,
                2,
                nameof(languageCode))
            .ToLowerInvariant();
        var translation = _translations.SingleOrDefault(
            item => item.LanguageCode == language);
        if (translation is not null && _translations.Count > 1)
        {
            _translations.Remove(translation);
        }
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        Status = MediaStatus.Archived;
        ArchivedAt = archivedAt;
        ProcessingStatus = MediaProcessingStatus.Archived;
    }

    public void BeginProcessing(Guid? userId = null)
    {
        ProcessingStatus = MediaProcessingStatus.Processing;
        CreatedByUserId ??= userId;
        ProcessingErrorCode = null;
        ProcessingErrorMessage = null;
    }

    public void CompleteProcessing(int width, int height, string provider, DateTimeOffset processedAt)
    {
        OriginalExtension = Path.GetExtension(OriginalFileName)?.ToLowerInvariant();
        OriginalWidth = width;
        OriginalHeight = height;
        StorageProvider = MediaGuard.Required(provider, 50, nameof(provider));
        ProcessingStatus = MediaProcessingStatus.Completed;
        ProcessedAt = processedAt;
    }

    public void FailProcessing(string code, string message, bool compensationRequired = false)
    {
        ProcessingStatus = compensationRequired
            ? MediaProcessingStatus.CompensationRequired
            : MediaProcessingStatus.Failed;
        ProcessingErrorCode = MediaGuard.Required(code, 100, nameof(code));
        ProcessingErrorMessage = MediaGuard.Required(message, 1000, nameof(message));
    }

    public void AddVariant(MediaVariant variant)
    {
        ArgumentNullException.ThrowIfNull(variant);
        if (variant.MediaAssetId != Id || _variants.Any(x => x.VariantType == variant.VariantType))
            throw new InvalidOperationException("A media variant type can only be added once.");
        _variants.Add(variant);
    }
}
