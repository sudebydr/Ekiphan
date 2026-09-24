using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Content;

public sealed class ContentPage : Entity
{
    private readonly List<ContentPageTranslation> _translations = [];

    private ContentPage()
    {
    }

    public ContentPage(Guid id, string code)
        : base(id)
    {
        UpdateCode(code);
    }

    public string Code { get; private set; } = string.Empty;

    public ContentStatus Status { get; private set; } = ContentStatus.Draft;

    public DateTimeOffset? PublishedAt { get; private set; }

    public IReadOnlyCollection<ContentPageTranslation> Translations =>
        _translations;

    public void UpdateCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length > 100 ||
            normalized.Any(item => !char.IsAsciiLetterOrDigit(item) &&
                item != '_'))
        {
            throw new ArgumentException(
                "Code must contain up to 100 ASCII letters, digits or underscores.",
                nameof(code));
        }

        Code = normalized;
    }

    public void SetStatus(ContentStatus status, DateTimeOffset now)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        if (status == ContentStatus.Published && _translations.Count == 0)
        {
            throw new InvalidOperationException(
                "Content cannot be published without a translation.");
        }

        Status = status;
        PublishedAt = status == ContentStatus.Published
            ? PublishedAt ?? now
            : null;
    }

    public void SetTranslation(
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
        var language = languageCode.Trim().ToLowerInvariant();
        var translation = _translations.SingleOrDefault(
            item => item.LanguageCode == language);
        if (translation is null)
        {
            _translations.Add(
                new ContentPageTranslation(
                    Id,
                    language,
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
                    openGraphImageMediaId));
            return;
        }

        translation.Update(
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

    public void RemoveTranslation(string languageCode)
    {
        var language = languageCode.Trim().ToLowerInvariant();
        var translation = _translations.SingleOrDefault(
            item => item.LanguageCode == language);
        if (translation is not null && _translations.Count > 1)
        {
            _translations.Remove(translation);
        }
    }
}
