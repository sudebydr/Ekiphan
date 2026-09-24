using System.Text.RegularExpressions;
using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Content;

public sealed partial class ContentPageTranslation
{
    private ContentPageTranslation()
    {
    }

    internal ContentPageTranslation(
        Guid contentPageId,
        string languageCode,
        string title,
        string slug,
        string? summary,
        string body,
        string? metaTitle,
        string? metaDescription,
        string? canonicalUrl,
        bool noIndex,
        bool noFollow,
        string? openGraphTitle = null,
        string? openGraphDescription = null,
        Guid? openGraphImageMediaId = null)
    {
        ContentPageId = contentPageId;
        LanguageCode = Language(languageCode);
        Update(
            title,
            slug,
            summary,
            body,
            metaTitle,
            metaDescription,
            canonicalUrl,
            noIndex,
            noFollow,
            openGraphTitle,
            openGraphDescription,
            openGraphImageMediaId);
    }

    public Guid ContentPageId { get; private set; }

    public string LanguageCode { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? Summary { get; private set; }

    public string Body { get; private set; } = string.Empty;

    public string? MetaTitle { get; private set; }

    public string? MetaDescription { get; private set; }

    public string? CanonicalUrl { get; private set; }

    public bool NoIndex { get; private set; }

    public bool NoFollow { get; private set; }

    public string? OpenGraphTitle { get; private set; }
    public string? OpenGraphDescription { get; private set; }
    public Guid? OpenGraphImageMediaId { get; private set; }

    internal void Update(
        string title,
        string slug,
        string? summary,
        string body,
        string? metaTitle,
        string? metaDescription,
        string? canonicalUrl,
        bool noIndex,
        bool noFollow,
        string? openGraphTitle = null,
        string? openGraphDescription = null,
        Guid? openGraphImageMediaId = null)
    {
        Title = Required(title, 200, nameof(title));
        Slug = SeoValue.Slug(slug, 200);
        Summary = Optional(summary, 500, nameof(summary));
        Body = Required(body, 50_000, nameof(body));
        MetaTitle = Optional(metaTitle, 70, nameof(metaTitle));
        MetaDescription =
            Optional(metaDescription, 170, nameof(metaDescription));
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

    private static string Language(string value)
    {
        var language = Required(value, 2, nameof(value)).ToLowerInvariant();
        if (language is not ("tr" or "en"))
        {
            throw new ArgumentException(
                "Only tr and en languages are supported.",
                nameof(value));
        }

        return language;
    }

    private static string SlugValue(string value)
    {
        var slug = Required(value, 200, nameof(value)).ToLowerInvariant();
        if (!SlugPattern().IsMatch(slug))
        {
            throw new ArgumentException(
                "Slug must contain lowercase letters, digits and single hyphens.",
                nameof(value));
        }

        return slug;
    }

    private static string? Canonical(string? value)
    {
        var canonical = Optional(value, 2048, nameof(value));
        if (canonical is null)
        {
            return null;
        }

        if (canonical.StartsWith('/') &&
            !canonical.StartsWith("//", StringComparison.Ordinal))
        {
            return canonical;
        }

        if (!Uri.TryCreate(canonical, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException(
                "Canonical URL must be site-relative or absolute HTTPS.",
                nameof(value));
        }

        return canonical;
    }

    private static string Required(
        string value,
        int maximumLength,
        string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static string? Optional(
        string? value,
        int maximumLength,
        string parameterName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();
}
