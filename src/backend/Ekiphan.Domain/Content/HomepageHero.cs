using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Content;

public sealed class HomepageHero : Entity
{
    private readonly List<HomepageHeroTranslation> _translations = [];

    private HomepageHero()
    {
    }

    public HomepageHero(Guid id, int sortOrder = 0) : base(id)
    {
        SortOrder = sortOrder;
    }

    public Guid? DesktopMediaId { get; private set; }
    public Guid? MobileMediaId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPublished { get; private set; }
    public DateTimeOffset? StartsAt { get; private set; }
    public DateTimeOffset? EndsAt { get; private set; }
    public IReadOnlyCollection<HomepageHeroTranslation> Translations => _translations;

    public void Configure(
        Guid? desktopMediaId,
        Guid? mobileMediaId,
        int sortOrder,
        DateTimeOffset? startsAt,
        DateTimeOffset? endsAt)
    {
        if (endsAt.HasValue && startsAt.HasValue && endsAt <= startsAt)
        {
            throw new ArgumentException("Hero end date must be after its start date.");
        }

        DesktopMediaId = desktopMediaId;
        MobileMediaId = mobileMediaId;
        SortOrder = sortOrder;
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    public void SetTranslation(
        string languageCode,
        string title,
        string? subtitle,
        string primaryCtaLabel,
        string primaryCtaUrl,
        string? secondaryCtaLabel,
        string? secondaryCtaUrl)
    {
        var language = languageCode.Trim().ToLowerInvariant();
        var translation = _translations.SingleOrDefault(
            item => item.LanguageCode == language);
        if (translation is null)
        {
            _translations.Add(new HomepageHeroTranslation(
                Id,
                language,
                title,
                subtitle,
                primaryCtaLabel,
                primaryCtaUrl,
                secondaryCtaLabel,
                secondaryCtaUrl));
            return;
        }

        translation.Update(
            language,
            title,
            subtitle,
            primaryCtaLabel,
            primaryCtaUrl,
            secondaryCtaLabel,
            secondaryCtaUrl);
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

    public void SetPublished(bool value)
    {
        if (value && _translations.Count == 0)
        {
            throw new InvalidOperationException("Hero requires a translation before publishing.");
        }

        IsPublished = value;
    }
}
