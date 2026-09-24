using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Content;

public sealed class PressReleaseTranslation
{
    private PressReleaseTranslation()
    {
    }

    public PressReleaseTranslation(
        Guid pressReleaseId,
        string languageCode,
        string title,
        string summary,
        string? body,
        string? slug = null,
        string? metaTitle = null,
        string? metaDescription = null,
        string? canonicalUrl = null,
        bool noIndex = false,
        bool noFollow = false,
        string? openGraphTitle = null,
        string? openGraphDescription = null,
        Guid? openGraphImageMediaId = null)
    {
        if (pressReleaseId == Guid.Empty)
        {
            throw new ArgumentException("Press release identifier is required.", nameof(pressReleaseId));
        }

        PressReleaseId = pressReleaseId;
        Update(
            languageCode, title, summary, body, slug, metaTitle, metaDescription,
            canonicalUrl, noIndex, noFollow, openGraphTitle,
            openGraphDescription, openGraphImageMediaId);
    }

    public Guid PressReleaseId { get; private set; }
    public string LanguageCode { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public string? Body { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? CanonicalUrl { get; private set; }
    public bool NoIndex { get; private set; }
    public bool NoFollow { get; private set; }
    public string? OpenGraphTitle { get; private set; }
    public string? OpenGraphDescription { get; private set; }
    public Guid? OpenGraphImageMediaId { get; private set; }

    internal void Update(
        string languageCode,
        string title,
        string summary,
        string? body,
        string? slug = null,
        string? metaTitle = null,
        string? metaDescription = null,
        string? canonicalUrl = null,
        bool noIndex = false,
        bool noFollow = false,
        string? openGraphTitle = null,
        string? openGraphDescription = null,
        Guid? openGraphImageMediaId = null)
    {
        LanguageCode = languageCode.Trim().ToLowerInvariant();
        if (LanguageCode is not ("tr" or "en"))
        {
            throw new ArgumentOutOfRangeException(nameof(languageCode));
        }

        Title = Required(title, 250, nameof(title));
        Summary = Required(summary, 1000, nameof(summary));
        Body = string.IsNullOrWhiteSpace(body) ? null : Required(body, 20000, nameof(body));
        Slug = string.IsNullOrWhiteSpace(slug)
            ? SeoValue.GenerateSlug(title, 250)
            : SeoValue.Slug(slug, 250);
        MetaTitle = SeoValue.Optional(metaTitle, 70, nameof(metaTitle));
        MetaDescription = SeoValue.Optional(metaDescription, 320, nameof(metaDescription));
        CanonicalUrl = SeoValue.Canonical(canonicalUrl);
        NoIndex = noIndex;
        NoFollow = noFollow;
        OpenGraphTitle = SeoValue.Optional(openGraphTitle, 95, nameof(openGraphTitle));
        OpenGraphDescription = SeoValue.Optional(
            openGraphDescription, 300, nameof(openGraphDescription));
        OpenGraphImageMediaId = !openGraphImageMediaId.HasValue ||
            openGraphImageMediaId.Value == Guid.Empty
                ? null
                : openGraphImageMediaId;
    }

    private static string Required(string value, int maximum, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        var normalized = value.Trim();
        return normalized.Length <= maximum
            ? normalized
            : throw new ArgumentOutOfRangeException(name);
    }
}
