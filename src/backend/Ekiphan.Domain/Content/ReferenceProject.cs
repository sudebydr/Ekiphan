using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Content;

public sealed class ReferenceProject : Entity
{
    private readonly List<ReferenceProjectTranslation> _translations = [];
    private readonly List<ReferenceProjectMedia> _media = [];
    private readonly List<ReferenceProjectProduct> _products = [];

    private ReferenceProject()
    {
    }

    public ReferenceProject(
        Guid id,
        string customerName,
        DateTimeOffset? projectDate = null,
        string? location = null,
        Guid? coverMediaAssetId = null,
        int sortOrder = 0,
        bool isFeatured = false,
        Guid? createdByUserId = null)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("Customer name is required.", nameof(customerName));

        CustomerName = customerName.Trim();
        ProjectDate = projectDate;
        Location = location?.Trim();
        CoverMediaAssetId = coverMediaAssetId;
        SortOrder = sortOrder;
        IsFeatured = isFeatured;
        WorkflowStatus = ContentWorkflowStatus.Draft;
        CreatedByUserId = createdByUserId ?? Guid.Empty;
        CreatedAt = DateTimeOffset.UtcNow;
        RowVersion = Array.Empty<byte>();
    }

    public string CustomerName { get; private set; } = string.Empty;
    public DateTimeOffset? ProjectDate { get; private set; }
    public string? Location { get; private set; }
    public Guid? CoverMediaAssetId { get; private set; }
    public ContentWorkflowStatus WorkflowStatus { get; private set; }
    public DateTimeOffset? PublishAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public Guid? PublishedByUserId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsFeatured { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public new DateTimeOffset CreatedAt { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public new DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public Guid? ArchivedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public IReadOnlyCollection<ReferenceProjectTranslation> Translations => _translations.AsReadOnly();
    public IReadOnlyCollection<ReferenceProjectMedia> Media => _media.AsReadOnly();
    public IReadOnlyCollection<ReferenceProjectProduct> Products => _products.AsReadOnly();

    public void UpdateIdentity(string customerName, DateTimeOffset? projectDate, string? location, Guid? coverMediaAssetId, bool isFeatured, Guid actorUserId)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("Customer name is required.", nameof(customerName));

        CustomerName = customerName.Trim();
        ProjectDate = projectDate;
        Location = location?.Trim();
        CoverMediaAssetId = coverMediaAssetId;
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
            PublishedByUserId = actorUserId;
            PublishAt = null;
        }
        else if (targetStatus == ContentWorkflowStatus.Archived)
        {
            ArchivedAt = DateTimeOffset.UtcNow;
            ArchivedByUserId = actorUserId;
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
        string? canonicalUrl = null,
        string? openGraphTitle = null,
        string? openGraphDescription = null,
        Guid? openGraphMediaAssetId = null)
    {
        var existing = _translations.FirstOrDefault(t => t.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.Update(title, slug, shortDescription, longDescription, metaTitle, metaDescription, canonicalUrl, openGraphTitle, openGraphDescription, openGraphMediaAssetId);
        }
        else
        {
            _translations.Add(new ReferenceProjectTranslation(Id, languageCode, title, slug, shortDescription, longDescription, metaTitle, metaDescription, canonicalUrl, openGraphTitle, openGraphDescription, openGraphMediaAssetId));
        }
    }

    public void AddMedia(Guid mediaAssetId, int sortOrder, bool isCover, string? captionTr = null, string? captionEn = null)
    {
        if (_media.Any(m => m.MediaAssetId == mediaAssetId))
            throw new InvalidOperationException("Media asset already attached to this reference project.");

        if (isCover)
        {
            foreach (var item in _media) item.SetCover(false);
            CoverMediaAssetId = mediaAssetId;
        }

        _media.Add(new ReferenceProjectMedia(Id, mediaAssetId, sortOrder, isCover, captionTr, captionEn));
    }

    public void RemoveMedia(Guid mediaAssetId)
    {
        var media = _media.FirstOrDefault(m => m.MediaAssetId == mediaAssetId);
        if (media is not null)
        {
            _media.Remove(media);
            if (CoverMediaAssetId == mediaAssetId)
            {
                CoverMediaAssetId = _media.FirstOrDefault(m => m.IsCover)?.MediaAssetId ?? _media.FirstOrDefault()?.MediaAssetId;
            }
        }
    }

    public void AddProduct(Guid productId, int sortOrder, string? description = null)
    {
        if (_products.Any(p => p.ProductId == productId))
            throw new InvalidOperationException("Product already attached to this reference project.");

        _products.Add(new ReferenceProjectProduct(Id, productId, sortOrder, description));
    }

    public void RemoveProduct(Guid productId)
    {
        var item = _products.FirstOrDefault(p => p.ProductId == productId);
        if (item is not null) _products.Remove(item);
    }
}

public sealed class ReferenceProjectTranslation
{
    private ReferenceProjectTranslation() { }

    internal ReferenceProjectTranslation(
        Guid referenceProjectId,
        string languageCode,
        string title,
        string slug,
        string? shortDescription,
        string? longDescription,
        string? metaTitle,
        string? metaDescription,
        string? canonicalUrl,
        string? openGraphTitle,
        string? openGraphDescription,
        Guid? openGraphMediaAssetId)
    {
        ReferenceProjectId = referenceProjectId;
        LanguageCode = languageCode.ToLowerInvariant().Trim();
        Update(title, slug, shortDescription, longDescription, metaTitle, metaDescription, canonicalUrl, openGraphTitle, openGraphDescription, openGraphMediaAssetId);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ReferenceProjectId { get; private set; }
    public string LanguageCode { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? ShortDescription { get; private set; }
    public string? LongDescription { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? CanonicalUrl { get; private set; }
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
        string? canonicalUrl,
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
        CanonicalUrl = canonicalUrl?.Trim();
        OpenGraphTitle = openGraphTitle?.Trim();
        OpenGraphDescription = openGraphDescription?.Trim();
        OpenGraphMediaAssetId = openGraphMediaAssetId;
    }
}

public sealed class ReferenceProjectMedia
{
    private ReferenceProjectMedia() { }

    internal ReferenceProjectMedia(Guid referenceProjectId, Guid mediaAssetId, int sortOrder, bool isCover, string? captionTr, string? captionEn)
    {
        Id = Guid.NewGuid();
        ReferenceProjectId = referenceProjectId;
        MediaAssetId = mediaAssetId;
        SortOrder = sortOrder;
        IsCover = isCover;
        CaptionTr = captionTr?.Trim();
        CaptionEn = captionEn?.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ReferenceProjectId { get; private set; }
    public Guid MediaAssetId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsCover { get; private set; }
    public string? CaptionTr { get; private set; }
    public string? CaptionEn { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    internal void SetCover(bool isCover) => IsCover = isCover;
    public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;
}

public sealed class ReferenceProjectProduct
{
    private ReferenceProjectProduct() { }

    internal ReferenceProjectProduct(Guid referenceProjectId, Guid productId, int sortOrder, string? description)
    {
        ReferenceProjectId = referenceProjectId;
        ProductId = productId;
        SortOrder = sortOrder;
        Description = description?.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid ReferenceProjectId { get; private set; }
    public Guid ProductId { get; private set; }
    public int SortOrder { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;
}
