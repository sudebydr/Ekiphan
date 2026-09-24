using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Content;

public sealed class PressRelease : Entity
{
    private readonly List<PressReleaseTranslation> _translations = [];

    private PressRelease()
    {
    }

    public PressRelease(Guid id, DateTimeOffset publishedAt) : base(id)
    {
        PublishedAt = publishedAt;
    }

    public Guid? CoverMediaId { get; private set; }
    public Guid? AttachmentMediaId { get; private set; }
    public DateTimeOffset PublishedAt { get; private set; }
    public bool IsPublished { get; private set; }
    public IReadOnlyCollection<PressReleaseTranslation> Translations => _translations;

    public void Configure(
        Guid? coverMediaId,
        Guid? attachmentMediaId,
        DateTimeOffset publishedAt)
    {
        CoverMediaId = EmptyToNull(coverMediaId);
        AttachmentMediaId = EmptyToNull(attachmentMediaId);
        PublishedAt = publishedAt;
    }

    public void SetTranslation(
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
        var language = languageCode.Trim().ToLowerInvariant();
        var translation = _translations.SingleOrDefault(item => item.LanguageCode == language);
        if (translation is null)
        {
            _translations.Add(new PressReleaseTranslation(
                Id, language, title, summary, body, slug, metaTitle,
                metaDescription, canonicalUrl, noIndex, noFollow,
                openGraphTitle, openGraphDescription, openGraphImageMediaId));
            return;
        }

        translation.Update(
            language, title, summary, body, slug, metaTitle, metaDescription,
            canonicalUrl, noIndex, noFollow, openGraphTitle,
            openGraphDescription, openGraphImageMediaId);
    }

    public void RemoveTranslation(string languageCode)
    {
        var language = languageCode.Trim().ToLowerInvariant();
        var translation = _translations.SingleOrDefault(item => item.LanguageCode == language);
        if (translation is not null && _translations.Count > 1)
        {
            _translations.Remove(translation);
        }
    }

    public void SetPublished(bool value)
    {
        if (value && _translations.Count == 0)
        {
            throw new InvalidOperationException(
                "Press release requires a translation before publishing.");
        }

        IsPublished = value;
    }

    private static Guid? EmptyToNull(Guid? value) =>
        value is null || value == Guid.Empty ? null : value;
}
