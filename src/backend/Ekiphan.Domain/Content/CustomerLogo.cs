using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Content;

public sealed class CustomerLogo : Entity
{
    private CustomerLogo() { }

    public CustomerLogo(
        Guid id,
        string name,
        Guid mediaAssetId,
        string? websiteUrl = null,
        string? altTextTr = null,
        string? altTextEn = null,
        int sortOrder = 0,
        bool isActive = true,
        bool isFeatured = false,
        Guid? createdByUserId = null)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Logo name is required.", nameof(name));
        if (mediaAssetId == Guid.Empty) throw new ArgumentException("MediaAssetId is required.", nameof(mediaAssetId));

        Name = name.Trim();
        MediaAssetId = mediaAssetId;
        WebsiteUrl = websiteUrl?.Trim();
        AltTextTr = altTextTr?.Trim();
        AltTextEn = altTextEn?.Trim();
        SortOrder = sortOrder;
        IsActive = isActive;
        IsFeatured = isFeatured;
        CreatedByUserId = createdByUserId ?? Guid.Empty;
        CreatedAt = DateTimeOffset.UtcNow;
        RowVersion = Array.Empty<byte>();
    }

    public string Name { get; private set; } = string.Empty;
    public string? WebsiteUrl { get; private set; }
    public Guid MediaAssetId { get; private set; }
    public string? AltTextTr { get; private set; }
    public string? AltTextEn { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsFeatured { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public new DateTimeOffset CreatedAt { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public new DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public void Update(string name, Guid mediaAssetId, string? websiteUrl, string? altTextTr, string? altTextEn, bool isActive, bool isFeatured, Guid actorUserId)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Logo name is required.", nameof(name));
        if (mediaAssetId == Guid.Empty) throw new ArgumentException("MediaAssetId is required.", nameof(mediaAssetId));

        Name = name.Trim();
        MediaAssetId = mediaAssetId;
        WebsiteUrl = websiteUrl?.Trim();
        AltTextTr = altTextTr?.Trim();
        AltTextEn = altTextEn?.Trim();
        IsActive = isActive;
        IsFeatured = isFeatured;
        UpdatedByUserId = actorUserId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;

    public void Archive(Guid actorUserId)
    {
        IsActive = false;
        ArchivedAt = DateTimeOffset.UtcNow;
        UpdatedByUserId = actorUserId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
