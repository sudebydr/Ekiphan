namespace Ekiphan.Domain.Media;

public sealed class BrandMedia
{
    private BrandMedia()
    {
    }

    public BrandMedia(
        Guid brandId,
        Guid mediaAssetId,
        BrandMediaRole role,
        int sortOrder = 0)
    {
        if (brandId == Guid.Empty || mediaAssetId == Guid.Empty)
        {
            throw new ArgumentException(
                "Brand and media asset identifiers are required.");
        }

        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role));
        }

        BrandId = brandId;
        MediaAssetId = mediaAssetId;
        Role = role;
        SortOrder = sortOrder;
    }

    public Guid BrandId { get; private set; }

    public Guid MediaAssetId { get; private set; }

    public BrandMediaRole Role { get; private set; }

    public int SortOrder { get; private set; }

    public void Update(int sortOrder) => SortOrder = sortOrder;
}
