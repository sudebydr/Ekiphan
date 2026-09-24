namespace Ekiphan.Domain.Media;

public sealed class ProductMedia
{
    private ProductMedia()
    {
    }

    public ProductMedia(
        Guid productId,
        Guid mediaAssetId,
        ProductMediaRole role,
        bool isDefault = false,
        int sortOrder = 0)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product identifier cannot be empty.",
                nameof(productId));
        }

        if (mediaAssetId == Guid.Empty)
        {
            throw new ArgumentException(
                "Media asset identifier cannot be empty.",
                nameof(mediaAssetId));
        }

        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role));
        }

        if (isDefault && role != ProductMediaRole.GalleryImage)
        {
            throw new ArgumentException(
                "Only a gallery image can be the default product media.",
                nameof(isDefault));
        }

        ProductId = productId;
        MediaAssetId = mediaAssetId;
        Role = role;
        IsDefault = isDefault;
        SortOrder = sortOrder;
    }

    public Guid ProductId { get; private set; }

    public Guid MediaAssetId { get; private set; }

    public ProductMediaRole Role { get; private set; }

    public bool IsDefault { get; private set; }

    public int SortOrder { get; private set; }

    public Guid? CreatedByImportBatchId { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public byte[] RowVersion { get; private set; } = [];

    public void Update(bool isDefault, int sortOrder)
    {
        if (isDefault && Role != ProductMediaRole.GalleryImage)
        {
            throw new ArgumentException(
                "Only a gallery image can be the default product media.",
                nameof(isDefault));
        }

        IsDefault = isDefault;
        SortOrder = sortOrder;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkImported(Guid batchId, DateTimeOffset now)
    {
        if (batchId == Guid.Empty) throw new ArgumentException("Import batch is required.", nameof(batchId));
        CreatedByImportBatchId = batchId;
        UpdatedAt = now;
    }
}
