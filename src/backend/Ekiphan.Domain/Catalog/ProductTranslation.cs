using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Catalog;

public sealed class ProductTranslation
{
    private ProductTranslation()
    {
    }

    internal ProductTranslation(
        Guid productId,
        string languageCode,
        string name,
        string slug,
        string? shortDescription,
        string? longDescription,
        string? metaTitle = null,
        string? metaDescription = null,
        string? canonicalUrl = null,
        bool noIndex = false,
        bool noFollow = false,
        string? openGraphTitle = null,
        string? openGraphDescription = null,
        Guid? openGraphImageMediaId = null)
    {
        ProductId = productId;
        LanguageCode = CatalogGuard.LanguageCode(languageCode);
        Name = CatalogGuard.Required(name, 250, nameof(name));
        Slug = SeoValue.Slug(slug);
        ShortDescription = shortDescription?.Trim();
        LongDescription = longDescription?.Trim();
        MetaTitle = Optional(metaTitle, 70, nameof(metaTitle));
        MetaDescription = Optional(
            metaDescription,
            320,
            nameof(metaDescription));
        CanonicalUrl = SeoValue.Canonical(canonicalUrl);
        NoIndex = noIndex;
        NoFollow = noFollow;
        OpenGraphTitle = SeoValue.Optional(openGraphTitle, 95, nameof(openGraphTitle));
        OpenGraphDescription = SeoValue.Optional(
            openGraphDescription, 300, nameof(openGraphDescription));
        OpenGraphImageMediaId = EmptyToNull(openGraphImageMediaId);
    }

    public Guid ProductId { get; private set; }

    public string LanguageCode { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? ShortDescription { get; private set; }

    public string? LongDescription { get; private set; }

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
        string? shortDescription,
        string? longDescription,
        string? metaTitle = null,
        string? metaDescription = null,
        string? canonicalUrl = null,
        bool noIndex = false,
        bool noFollow = false,
        string? openGraphTitle = null,
        string? openGraphDescription = null,
        Guid? openGraphImageMediaId = null)
    {
        Name = CatalogGuard.Required(name, 250, nameof(name));
        Slug = SeoValue.Slug(slug);
        ShortDescription = shortDescription?.Trim();
        LongDescription = longDescription?.Trim();
        MetaTitle = Optional(metaTitle, 70, nameof(metaTitle));
        MetaDescription = Optional(
            metaDescription,
            320,
            nameof(metaDescription));
        CanonicalUrl = SeoValue.Canonical(canonicalUrl);
        NoIndex = noIndex;
        NoFollow = noFollow;
        OpenGraphTitle = SeoValue.Optional(openGraphTitle, 95, nameof(openGraphTitle));
        OpenGraphDescription = SeoValue.Optional(
            openGraphDescription, 300, nameof(openGraphDescription));
        OpenGraphImageMediaId = EmptyToNull(openGraphImageMediaId);
    }

    private static string? Optional(
        string? value,
        int maximumLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static Guid? EmptyToNull(Guid? value) =>
        value is null || value == Guid.Empty ? null : value;
}
