using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Content;

public sealed class FooterColumn : Entity
{
    private readonly List<FooterLink> _links = [];

    private FooterColumn() { }

    public FooterColumn(
        Guid id,
        string code,
        string titleTr,
        string titleEn,
        int sortOrder = 0,
        bool isActive = true)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(titleTr)) throw new ArgumentException("TitleTr is required.", nameof(titleTr));

        Code = code.Trim().ToUpperInvariant();
        TitleTr = titleTr.Trim();
        TitleEn = string.IsNullOrWhiteSpace(titleEn) ? titleTr.Trim() : titleEn.Trim();
        SortOrder = sortOrder;
        IsActive = isActive;
        CreatedAt = DateTimeOffset.UtcNow;
        RowVersion = Array.Empty<byte>();
    }

    public string Code { get; private set; } = string.Empty;
    public string TitleTr { get; private set; } = string.Empty;
    public string TitleEn { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public new DateTimeOffset CreatedAt { get; private set; }
    public new DateTimeOffset? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public IReadOnlyCollection<FooterLink> Links => _links.AsReadOnly();

    public void Update(string titleTr, string titleEn, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(titleTr)) throw new ArgumentException("TitleTr is required.", nameof(titleTr));

        TitleTr = titleTr.Trim();
        TitleEn = string.IsNullOrWhiteSpace(titleEn) ? titleTr.Trim() : titleEn.Trim();
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;

    public FooterLink AddLink(string labelTr, string labelEn, string url, BannerLinkTarget linkTarget, int sortOrder, bool isActive = true)
    {
        var link = new FooterLink(Id, labelTr, labelEn, url, linkTarget, sortOrder, isActive);
        _links.Add(link);
        return link;
    }

    public void RemoveLink(Guid linkId)
    {
        var item = _links.FirstOrDefault(l => l.Id == linkId);
        if (item is not null) _links.Remove(item);
    }
}

public sealed class FooterLink : Entity
{
    private FooterLink() { }

    internal FooterLink(
        Guid footerColumnId,
        string labelTr,
        string labelEn,
        string url,
        BannerLinkTarget linkTarget,
        int sortOrder,
        bool isActive = true)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(labelTr)) throw new ArgumentException("LabelTr is required.", nameof(labelTr));
        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("Url is required.", nameof(url));

        FooterColumnId = footerColumnId;
        LabelTr = labelTr.Trim();
        LabelEn = string.IsNullOrWhiteSpace(labelEn) ? labelTr.Trim() : labelEn.Trim();
        Url = url.Trim();
        LinkTarget = linkTarget;
        SortOrder = sortOrder;
        IsActive = isActive;
        CreatedAt = DateTimeOffset.UtcNow;
        RowVersion = Array.Empty<byte>();
    }

    public Guid FooterColumnId { get; private set; }
    public string LabelTr { get; private set; } = string.Empty;
    public string LabelEn { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public BannerLinkTarget LinkTarget { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public new DateTimeOffset CreatedAt { get; private set; }
    public new DateTimeOffset? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public void Update(string labelTr, string labelEn, string url, BannerLinkTarget linkTarget, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(labelTr)) throw new ArgumentException("LabelTr is required.", nameof(labelTr));
        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("Url is required.", nameof(url));

        LabelTr = labelTr.Trim();
        LabelEn = string.IsNullOrWhiteSpace(labelEn) ? labelTr.Trim() : labelEn.Trim();
        Url = url.Trim();
        LinkTarget = linkTarget;
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;
}
