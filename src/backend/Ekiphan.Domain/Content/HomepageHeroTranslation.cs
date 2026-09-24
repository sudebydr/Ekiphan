namespace Ekiphan.Domain.Content;

public sealed class HomepageHeroTranslation
{
    private HomepageHeroTranslation()
    {
    }

    public HomepageHeroTranslation(
        Guid homepageHeroId,
        string languageCode,
        string title,
        string? subtitle,
        string primaryCtaLabel,
        string primaryCtaUrl,
        string? secondaryCtaLabel,
        string? secondaryCtaUrl)
    {
        if (homepageHeroId == Guid.Empty)
        {
            throw new ArgumentException("Hero identifier is required.", nameof(homepageHeroId));
        }

        HomepageHeroId = homepageHeroId;
        Update(
            languageCode,
            title,
            subtitle,
            primaryCtaLabel,
            primaryCtaUrl,
            secondaryCtaLabel,
            secondaryCtaUrl);
    }

    public Guid HomepageHeroId { get; private set; }
    public string LanguageCode { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Subtitle { get; private set; }
    public string PrimaryCtaLabel { get; private set; } = string.Empty;
    public string PrimaryCtaUrl { get; private set; } = string.Empty;
    public string? SecondaryCtaLabel { get; private set; }
    public string? SecondaryCtaUrl { get; private set; }

    internal void Update(
        string languageCode,
        string title,
        string? subtitle,
        string primaryCtaLabel,
        string primaryCtaUrl,
        string? secondaryCtaLabel,
        string? secondaryCtaUrl)
    {
        LanguageCode = languageCode.Trim().ToLowerInvariant();
        if (LanguageCode is not ("tr" or "en"))
        {
            throw new ArgumentOutOfRangeException(nameof(languageCode));
        }

        Title = Required(title, 200, nameof(title));
        Subtitle = Optional(subtitle, 500, nameof(subtitle));
        PrimaryCtaLabel = Required(primaryCtaLabel, 100, nameof(primaryCtaLabel));
        PrimaryCtaUrl = InternalUrl(primaryCtaUrl, nameof(primaryCtaUrl));
        SecondaryCtaLabel = Optional(secondaryCtaLabel, 100, nameof(secondaryCtaLabel));
        SecondaryCtaUrl = secondaryCtaLabel is null
            ? null
            : InternalUrl(secondaryCtaUrl, nameof(secondaryCtaUrl));
    }

    private static string Required(string value, int maximum, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        var normalized = value.Trim();
        return normalized.Length <= maximum
            ? normalized
            : throw new ArgumentOutOfRangeException(name);
    }

    private static string? Optional(string? value, int maximum, string name) =>
        string.IsNullOrWhiteSpace(value) ? null : Required(value, maximum, name);

    private static string InternalUrl(string? value, string name)
    {
        var url = Required(value!, 2048, name);
        return (url.StartsWith('/') && !url.StartsWith("//", StringComparison.Ordinal)) ||
            (url.StartsWith('#') && url.Length > 1)
            ? url
            : throw new ArgumentException("CTA URL must be site-relative or an anchor.", name);
    }
}
