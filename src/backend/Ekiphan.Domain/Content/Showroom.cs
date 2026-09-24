using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Content;

public sealed class Showroom : Entity
{
    private readonly List<ShowroomTranslation> _translations = [];
    private readonly List<ShowroomMedia> _media = [];
    private readonly List<ShowroomHotspot> _hotspots = [];

    private Showroom() { }

    public Showroom(
        Guid id,
        Guid? coverMediaAssetId = null,
        string? virtualTourUrl = null,
        string? embedCode = null,
        int sortOrder = 0,
        bool isFeatured = false,
        Guid? createdByUserId = null)
        : base(id)
    {
        CoverMediaAssetId = coverMediaAssetId;
        VirtualTourUrl = virtualTourUrl?.Trim();
        EmbedCode = embedCode?.Trim();
        SortOrder = sortOrder;
        IsFeatured = isFeatured;
        WorkflowStatus = ContentWorkflowStatus.Draft;
        CreatedByUserId = createdByUserId ?? Guid.Empty;
        CreatedAt = DateTimeOffset.UtcNow;
        RowVersion = Array.Empty<byte>();
    }

    public Guid? CoverMediaAssetId { get; private set; }
    public string? VirtualTourUrl { get; private set; }
    public string? EmbedCode { get; private set; }
    public ContentWorkflowStatus WorkflowStatus { get; private set; }
    public DateTimeOffset? PublishAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsFeatured { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public new DateTimeOffset CreatedAt { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public new DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public IReadOnlyCollection<ShowroomTranslation> Translations => _translations.AsReadOnly();
    public IReadOnlyCollection<ShowroomMedia> Media => _media.AsReadOnly();
    public IReadOnlyCollection<ShowroomHotspot> Hotspots => _hotspots.AsReadOnly();

    public void Update(Guid? coverMediaAssetId, string? virtualTourUrl, string? embedCode, bool isFeatured, Guid actorUserId)
    {
        CoverMediaAssetId = coverMediaAssetId;
        VirtualTourUrl = virtualTourUrl?.Trim();
        EmbedCode = embedCode?.Trim();
        IsFeatured = isFeatured;
        UpdatedByUserId = actorUserId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetWorkflowStatus(ContentWorkflowStatus targetStatus, DateTimeOffset? publishAt, Guid actorUserId)
    {
        WorkflowStatus = targetStatus;
        UpdatedByUserId = actorUserId;
        UpdatedAt = DateTimeOffset.UtcNow;

        if (targetStatus == ContentWorkflowStatus.Scheduled)
        {
            PublishAt = publishAt;
        }
        else if (targetStatus == ContentWorkflowStatus.Published)
        {
            PublishedAt = DateTimeOffset.UtcNow;
            PublishAt = null;
        }
        else if (targetStatus == ContentWorkflowStatus.Archived)
        {
            ArchivedAt = DateTimeOffset.UtcNow;
        }
    }

    public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;

    public void SetTranslation(
        string languageCode,
        string title,
        string slug,
        string? shortDescription = null,
        string? longDescription = null,
        string? metaTitle = null,
        string? metaDescription = null,
        string? openGraphTitle = null,
        string? openGraphDescription = null,
        Guid? openGraphMediaAssetId = null)
    {
        var existing = _translations.FirstOrDefault(t => t.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.Update(title, slug, shortDescription, longDescription, metaTitle, metaDescription, openGraphTitle, openGraphDescription, openGraphMediaAssetId);
        }
        else
        {
            _translations.Add(new ShowroomTranslation(Id, languageCode, title, slug, shortDescription, longDescription, metaTitle, metaDescription, openGraphTitle, openGraphDescription, openGraphMediaAssetId));
        }
    }

    public void AddMedia(Guid mediaAssetId, int sortOrder, string? captionTr = null, string? captionEn = null)
    {
        if (_media.Any(m => m.MediaAssetId == mediaAssetId))
            throw new InvalidOperationException("Media asset already attached to showroom.");

        _media.Add(new ShowroomMedia(Id, mediaAssetId, sortOrder, captionTr, captionEn));
    }

    public void RemoveMedia(Guid mediaAssetId)
    {
        var item = _media.FirstOrDefault(m => m.MediaAssetId == mediaAssetId);
        if (item is not null) _media.Remove(item);
    }

    public ShowroomHotspot AddHotspot(
        Guid? productId,
        string? titleTr,
        string? titleEn,
        string? descriptionTr,
        string? descriptionEn,
        double positionX,
        double positionY,
        double positionZ,
        string sceneIdentifier,
        int sortOrder,
        bool isActive = true)
    {
        if (_hotspots.Any(h => h.SceneIdentifier == sceneIdentifier && Math.Abs(h.PositionX - positionX) < 0.001 && Math.Abs(h.PositionY - positionY) < 0.001 && Math.Abs(h.PositionZ - positionZ) < 0.001))
            throw new InvalidOperationException("A hotspot already exists at these coordinates in this scene.");

        var hotspot = new ShowroomHotspot(Id, productId, titleTr, titleEn, descriptionTr, descriptionEn, positionX, positionY, positionZ, sceneIdentifier, sortOrder, isActive);
        _hotspots.Add(hotspot);
        return hotspot;
    }

    public void RemoveHotspot(Guid hotspotId)
    {
        var item = _hotspots.FirstOrDefault(h => h.Id == hotspotId);
        if (item is not null) _hotspots.Remove(item);
    }
}

public sealed class ShowroomTranslation
{
    private ShowroomTranslation() { }

    internal ShowroomTranslation(
        Guid showroomId,
        string languageCode,
        string title,
        string slug,
        string? shortDescription,
        string? longDescription,
        string? metaTitle,
        string? metaDescription,
        string? openGraphTitle,
        string? openGraphDescription,
        Guid? openGraphMediaAssetId)
    {
        ShowroomId = showroomId;
        LanguageCode = languageCode.ToLowerInvariant().Trim();
        Update(title, slug, shortDescription, longDescription, metaTitle, metaDescription, openGraphTitle, openGraphDescription, openGraphMediaAssetId);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ShowroomId { get; private set; }
    public string LanguageCode { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? ShortDescription { get; private set; }
    public string? LongDescription { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? OpenGraphTitle { get; private set; }
    public string? OpenGraphDescription { get; private set; }
    public Guid? OpenGraphMediaAssetId { get; private set; }

    internal void Update(
        string title,
        string slug,
        string? shortDescription,
        string? longDescription,
        string? metaTitle,
        string? metaDescription,
        string? openGraphTitle,
        string? openGraphDescription,
        Guid? openGraphMediaAssetId)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug is required.", nameof(slug));

        Title = title.Trim();
        Slug = slug.ToLowerInvariant().Trim();
        ShortDescription = shortDescription?.Trim();
        LongDescription = longDescription?.Trim();
        MetaTitle = metaTitle?.Trim();
        MetaDescription = metaDescription?.Trim();
        OpenGraphTitle = openGraphTitle?.Trim();
        OpenGraphDescription = openGraphDescription?.Trim();
        OpenGraphMediaAssetId = openGraphMediaAssetId;
    }
}

public sealed class ShowroomMedia
{
    private ShowroomMedia() { }

    internal ShowroomMedia(Guid showroomId, Guid mediaAssetId, int sortOrder, string? captionTr, string? captionEn)
    {
        Id = Guid.NewGuid();
        ShowroomId = showroomId;
        MediaAssetId = mediaAssetId;
        SortOrder = sortOrder;
        CaptionTr = captionTr?.Trim();
        CaptionEn = captionEn?.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ShowroomId { get; private set; }
    public Guid MediaAssetId { get; private set; }
    public int SortOrder { get; private set; }
    public string? CaptionTr { get; private set; }
    public string? CaptionEn { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;
}

public sealed class ShowroomHotspot : Entity
{
    private ShowroomHotspot() { }

    internal ShowroomHotspot(
        Guid showroomId,
        Guid? productId,
        string? titleTr,
        string? titleEn,
        string? descriptionTr,
        string? descriptionEn,
        double positionX,
        double positionY,
        double positionZ,
        string sceneIdentifier,
        int sortOrder,
        bool isActive = true)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(sceneIdentifier))
            throw new ArgumentException("SceneIdentifier is required.", nameof(sceneIdentifier));

        ShowroomId = showroomId;
        ProductId = productId;
        TitleTr = titleTr?.Trim();
        TitleEn = titleEn?.Trim();
        DescriptionTr = descriptionTr?.Trim();
        DescriptionEn = descriptionEn?.Trim();
        PositionX = positionX;
        PositionY = positionY;
        PositionZ = positionZ;
        SceneIdentifier = sceneIdentifier.Trim();
        SortOrder = sortOrder;
        IsActive = isActive;
        CreatedAt = DateTimeOffset.UtcNow;
        RowVersion = Array.Empty<byte>();
    }

    public Guid ShowroomId { get; private set; }
    public Guid? ProductId { get; private set; }
    public string? TitleTr { get; private set; }
    public string? TitleEn { get; private set; }
    public string? DescriptionTr { get; private set; }
    public string? DescriptionEn { get; private set; }
    public double PositionX { get; private set; }
    public double PositionY { get; private set; }
    public double PositionZ { get; private set; }
    public string SceneIdentifier { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public new DateTimeOffset CreatedAt { get; private set; }
    public new DateTimeOffset? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public void Update(
        Guid? productId,
        string? titleTr,
        string? titleEn,
        string? descriptionTr,
        string? descriptionEn,
        double positionX,
        double positionY,
        double positionZ,
        string sceneIdentifier,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(sceneIdentifier))
            throw new ArgumentException("SceneIdentifier is required.", nameof(sceneIdentifier));

        ProductId = productId;
        TitleTr = titleTr?.Trim();
        TitleEn = titleEn?.Trim();
        DescriptionTr = descriptionTr?.Trim();
        DescriptionEn = descriptionEn?.Trim();
        PositionX = positionX;
        PositionY = positionY;
        PositionZ = positionZ;
        SceneIdentifier = sceneIdentifier.Trim();
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;
}
