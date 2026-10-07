using System.Security.Cryptography;
using Ekiphan.Domain.Media;

namespace Ekiphan.Application.Media;

public sealed class MediaUploadService(
    IMediaFileSignatureValidator signatureValidator,
    IMediaThreatScanner threatScanner,
    IMediaFileStorage storage,
    IMediaAssetRepository repository,
    TimeProvider timeProvider)
{
    public async Task<UploadedMediaResult> UploadAsync(
        UploadMediaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Content);
        if (command.AssetType == MediaAssetType.ExternalVideo)
        {
            throw new ArgumentException(
                "External videos must be registered by URL.",
                nameof(command));
        }

        if (!string.Equals(
                command.FileName,
                Path.GetFileName(command.FileName),
                StringComparison.Ordinal))
        {
            throw new UnsafeMediaFileException(
                "The original file name cannot contain a path.");
        }

        if (!storage.IsConfigured)
        {
            throw new MediaUploadUnavailableException(
                "Media file storage is not configured.");
        }

        if (!command.Content.CanSeek ||
            command.Length <= 0 ||
            command.Content.Length != command.Length)
        {
            throw new UnsafeMediaFileException(
                "The uploaded stream length is invalid.");
        }

        var maximumLength = command.AssetType switch
        {
            MediaAssetType.Image => 4L * 1024 * 1024,
            MediaAssetType.Pdf => 500L * 1024 * 1024,
            _ => 50L * 1024 * 1024,
        };
        if (command.Length > maximumLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                $"The selected media type cannot exceed {maximumLength} bytes.");
        }

        if (!signatureValidator.IsValid(
                command.Content,
                command.AssetType,
                command.ContentType,
                command.FileName))
        {
            throw new UnsafeMediaFileException(
                "File signature, MIME type and extension do not match.");
        }

        command.Content.Position = 0;
        var scanStatus = await threatScanner.ScanAsync(
            command.Content,
            command.FileName,
            command.ContentType,
            cancellationToken);
        if (scanStatus == MediaThreatScanStatus.Unavailable)
        {
            throw new MediaUploadUnavailableException(
                "Media threat scanning is unavailable.");
        }

        if (scanStatus == MediaThreatScanStatus.ThreatFound)
        {
            throw new UnsafeMediaFileException(
                "The uploaded file was rejected by threat scanning.");
        }

        command.Content.Position = 0;
        var checksum = Convert.ToHexString(
            await SHA256.HashDataAsync(
                command.Content,
                cancellationToken));
        var extension = Path.GetExtension(command.FileName).ToLowerInvariant();
        var now = timeProvider.GetUtcNow();
        var storageKey =
            $"media/{now.Year:D4}/{now.Month:D2}/" +
            $"{Guid.NewGuid():N}{extension}";
        var assetId = Guid.NewGuid();
        var asset = MediaAsset.CreateFile(
            assetId,
            command.AssetType,
            command.FileName,
            storageKey,
            command.ContentType,
            command.Length,
            checksum,
            storage.ProviderName);
        asset.AddTranslation(
            command.LanguageCode,
            command.Title,
            command.AltText,
            command.Description);

        try
        {
            command.Content.Position = 0;
            await storage.SaveAsync(
                storageKey,
                command.Content,
                cancellationToken);
            await repository.AddAsync(asset, cancellationToken);
        }
        catch
        {
            await storage.DeleteAsync(storageKey, CancellationToken.None);
            throw;
        }

        return new UploadedMediaResult(
            assetId,
            asset.AssetType,
            asset.OriginalFileName!,
            asset.StorageKey!,
            asset.MimeType!,
            asset.FileSizeBytes!.Value,
            asset.Sha256Checksum!);
    }
}
