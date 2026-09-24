using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Catalog;

public sealed class CategoryTranslation
{
    private CategoryTranslation()
    {
    }

    internal CategoryTranslation(
        Guid categoryId,
        string languageCode,
        string name,
        string slug,
        string? description,
        string? metaTitle = null,
        string? metaDescription = null,
        string? canonicalUrl = null,
        bool noIndex = false,
        bool noFollow = false,
        string? openGraphTitle = null,
        string? openGraphDescription = null,
        Guid? openGraphImageMediaId = null)
    {
        CategoryId = categoryId;
        LanguageCode = CatalogGuard.LanguageCode(languageCode);
        Name = CatalogGuard.Required(name, 200, nameof(name));
        Slug = SeoValue.Slug(slug, 250);
        Description = description?.Trim();
        ApplySeo(metaTitle, metaDescription, canonicalUrl, noIndex, noFollow,
            openGraphTitle, openGraphDescription, openGraphImageMediaId);
    }

    public Guid CategoryId { get; private set; }

    public string LanguageCode { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? CanonicalUrl { get; private set; }
    public bool NoIndex { get; private set; }
    public bool NoFollow { get; private set; }
    public string? OpenGraphTitle { get; private set; }
    public string? OpenGraphDescription { get; private set; }
    public Guid? OpenGraphImageMediaId { get; private set; }

    internal void Update(
        string name,
        string slug,
        string? description,
        string? metaTitle = null,
        string? metaDescription = null,
        string? canonicalUrl = null,
        bool noIndex = false,
        bool noFollow = false,
        string? openGraphTitle = null,
        string? openGraphDescription = null,
        Guid? openGraphImageMediaId = null)
    {
        Name = CatalogGuard.Required(name, 200, nameof(name));
        Slug = SeoValue.Slug(slug, 250);
        Description = description?.Trim();
        ApplySeo(metaTitle, metaDescription, canonicalUrl, noIndex, noFollow,
            openGraphTitle, openGraphDescription, openGraphImageMediaId);
    }

    private void ApplySeo(
        string? metaTitle,
        string? metaDescription,
        string? canonicalUrl,
        bool noIndex,
        bool noFollow,
        string? openGraphTitle,
        string? openGraphDescription,
        Guid? openGraphImageMediaId)
    {
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
}
