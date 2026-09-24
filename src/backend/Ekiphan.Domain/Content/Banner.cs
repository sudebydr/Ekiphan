using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Content;

public enum BannerPlacement
{
    HomeHero = 1,
    HomeSecondary = 2,
    CatalogTop = 3,
    CatalogBottom = 4,
    BrandPage = 5,
    ReferencePage = 6,
    ShowroomPage = 7,
    GlobalAnnouncement = 8
}

public enum BannerLinkTarget
{
    Self = 1,
    Blank = 2
}

public sealed class BannerGroup : Entity
{
    private readonly List<Banner> _banners = [];

    private BannerGroup() { }

    public BannerGroup(Guid id, string code, string name, BannerPlacement placement, bool isActive = true)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Placement = placement;
        IsActive = isActive;
        CreatedAt = DateTimeOffset.UtcNow;
        RowVersion = Array.Empty<byte>();
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public BannerPlacement Placement { get; private set; }
    public bool IsActive { get; private set; }
    public new DateTimeOffset CreatedAt { get; private set; }
    public new DateTimeOffset? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public IReadOnlyCollection<Banner> Banners => _banners.AsReadOnly();

    public void Update(string name, BannerPlacement placement, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));

        Name = name.Trim();
        Placement = placement;
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class Banner : Entity
{
    private readonly List<BannerTranslation> _translations = [];

    private Banner() { }

    public Banner(
        Guid id,
        Guid bannerGroupId,
        Guid desktopMediaAssetId,
        Guid? mobileMediaAssetId = null,
        string? linkUrl = null,
        BannerLinkTarget linkTarget = BannerLinkTarget.Self,
        DateTimeOffset? publishAt = null,
        DateTimeOffset? publishEndAt = null,
        int sortOrder = 0,
        bool isActive = true,
        Guid? createdByUserId = null)
        : base(id)
    {
        if (bannerGroupId == Guid.Empty) throw new ArgumentException("BannerGroupId is required.", nameof(bannerGroupId));
        if (desktopMediaAssetId == Guid.Empty) throw new ArgumentException("DesktopMediaAssetId is required.", nameof(desktopMediaAssetId));
        if (publishAt.HasValue && publishEndAt.HasValue && publishEndAt.Value <= publishAt.Value)
            throw new ArgumentException("PublishEndAt must be after PublishAt.", nameof(publishEndAt));

        BannerGroupId = bannerGroupId;
        DesktopMediaAssetId = desktopMediaAssetId;
        MobileMediaAssetId = mobileMediaAssetId;
        LinkUrl = linkUrl?.Trim();
        LinkTarget = linkTarget;
        WorkflowStatus = ContentWorkflowStatus.Draft;
        PublishAt = publishAt;
        PublishEndAt = publishEndAt;
        SortOrder = sortOrder;
        IsActive = isActive;
        CreatedByUserId = createdByUserId ?? Guid.Empty;
        CreatedAt = DateTimeOffset.UtcNow;
        RowVersion = Array.Empty<byte>();
    }

    public Guid BannerGroupId { get; private set; }
    public Guid DesktopMediaAssetId { get; private set; }
    public Guid? MobileMediaAssetId { get; private set; }
    public string? LinkUrl { get; private set; }
    public BannerLinkTarget LinkTarget { get; private set; }
    public ContentWorkflowStatus WorkflowStatus { get; private set; }
    public DateTimeOffset? PublishAt { get; private set; }
    public DateTimeOffset? PublishEndAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public new DateTimeOffset CreatedAt { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public new DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public IReadOnlyCollection<BannerTranslation> Translations => _translations.AsReadOnly();

    public void Update(
        Guid desktopMediaAssetId,
        Guid? mobileMediaAssetId,
        string? linkUrl,
        BannerLinkTarget linkTarget,
        DateTimeOffset? publishAt,
        DateTimeOffset? publishEndAt,
        bool isActive,
        Guid actorUserId)
    {
        if (desktopMediaAssetId == Guid.Empty) throw new ArgumentException("DesktopMediaAssetId is required.", nameof(desktopMediaAssetId));
        if (publishAt.HasValue && publishEndAt.HasValue && publishEndAt.Value <= publishAt.Value)
            throw new ArgumentException("PublishEndAt must be after PublishAt.", nameof(publishEndAt));

        DesktopMediaAssetId = desktopMediaAssetId;
        MobileMediaAssetId = mobileMediaAssetId;
        LinkUrl = linkUrl?.Trim();
        LinkTarget = linkTarget;
        PublishAt = publishAt;
        PublishEndAt = publishEndAt;
        IsActive = isActive;
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
        }
        else if (targetStatus == ContentWorkflowStatus.Archived)
        {
            ArchivedAt = DateTimeOffset.UtcNow;
            IsActive = false;
        }
    }

    public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;

    public void SetTranslation(
        string languageCode,
        string? title = null,
        string? subtitle = null,
        string? description = null,
        string? ctaText = null,
        string? accessibleLabel = null)
    {
        var existing = _translations.FirstOrDefault(t => t.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.Update(title, subtitle, description, ctaText, accessibleLabel);
        }
        else
        {
            _translations.Add(new BannerTranslation(Id, languageCode, title, subtitle, description, ctaText, accessibleLabel));
        }
    }
}

public sealed class BannerTranslation
{
    private BannerTranslation() { }

    internal BannerTranslation(
        Guid bannerId,
        string languageCode,
        string? title,
        string? subtitle,
        string? description,
        string? ctaText,
        string? accessibleLabel)
    {
        BannerId = bannerId;
        LanguageCode = languageCode.ToLowerInvariant().Trim();
        Update(title, subtitle, description, ctaText, accessibleLabel);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid BannerId { get; private set; }
    public string LanguageCode { get; private set; } = string.Empty;
    public string? Title { get; private set; }
    public string? Subtitle { get; private set; }
    public string? Description { get; private set; }
    public string? CtaText { get; private set; }
    public string? AccessibleLabel { get; private set; }

    internal void Update(string? title, string? subtitle, string? description, string? ctaText, string? accessibleLabel)
    {
        Title = title?.Trim();
        Subtitle = subtitle?.Trim();
        Description = description?.Trim();
        CtaText = ctaText?.Trim();
        AccessibleLabel = accessibleLabel?.Trim();
    }
}
