namespace Ekiphan.Domain.Media;

public sealed class CategoryMedia
{
    private CategoryMedia()
    {
    }

    public CategoryMedia(
        Guid categoryId,
        Guid mediaAssetId,
        CategoryMediaRole role)
    {
        if (categoryId == Guid.Empty || mediaAssetId == Guid.Empty)
        {
            throw new ArgumentException(
                "Category and media asset identifiers are required.");
        }

        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role));
        }

        CategoryId = categoryId;
        MediaAssetId = mediaAssetId;
        Role = role;
    }

    public Guid CategoryId { get; private set; }

    public Guid MediaAssetId { get; private set; }

    public CategoryMediaRole Role { get; private set; }

    public void UpdateMedia(Guid mediaAssetId)
    {
        if (mediaAssetId == Guid.Empty)
        {
            throw new ArgumentException(
                "Media asset identifier is required.",
                nameof(mediaAssetId));
        }

        MediaAssetId = mediaAssetId;
    }
}
