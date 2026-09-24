using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Media;

public sealed class MediaVariant : Entity
{
    private MediaVariant() { }

    private MediaVariant(Guid id) : base(id) { }

    public Guid MediaAssetId { get; private set; }
    public MediaVariantType VariantType { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public long FileSizeBytes { get; private set; }
    public string MimeType { get; private set; } = "image/webp";
    public string Extension { get; private set; } = ".webp";
    public int Quality { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public byte[] RowVersion { get; private set; } = [];

    public static MediaVariant Create(Guid mediaAssetId, MediaVariantType type,
        int width, int height, long fileSizeBytes, int quality, string storageKey) =>
        new(Guid.NewGuid())
        {
            MediaAssetId = mediaAssetId,
            VariantType = type,
            Width = width,
            Height = height,
            FileSizeBytes = fileSizeBytes,
            Quality = quality,
            StorageKey = MediaGuard.StorageKey(storageKey),
        };
}
