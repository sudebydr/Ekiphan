namespace Ekiphan.Application.Content;

public sealed record SaveHomepageHeroTranslation(
    string LanguageCode,
    string Title,
    string? Subtitle,
    string PrimaryCtaLabel,
    string PrimaryCtaUrl,
    string? SecondaryCtaLabel,
    string? SecondaryCtaUrl);

public sealed record SaveHomepageHeroCommand(
    Guid? DesktopMediaId,
    Guid? MobileMediaId,
    int SortOrder,
    bool IsPublished,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    IReadOnlyList<SaveHomepageHeroTranslation> Translations);

public sealed record AdminHomepageHero(
    Guid Id,
    Guid? DesktopMediaId,
    Guid? MobileMediaId,
    int SortOrder,
    bool IsPublished,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    IReadOnlyList<SaveHomepageHeroTranslation> Translations);

public sealed record PublicHomepageHero(
    Guid Id,
    string Title,
    string? Subtitle,
    string PrimaryCtaLabel,
    string PrimaryCtaUrl,
    string? SecondaryCtaLabel,
    string? SecondaryCtaUrl,
    string? DesktopMediaUrl,
    string? MobileMediaUrl);

public interface IHomepageHeroService
{
    Task<IReadOnlyList<AdminHomepageHero>> GetAdminAsync(
        CancellationToken cancellationToken = default);
    Task<AdminHomepageHero> CreateAsync(
        SaveHomepageHeroCommand command,
        CancellationToken cancellationToken = default);
    Task<AdminHomepageHero?> UpdateAsync(
        Guid id,
        SaveHomepageHeroCommand command,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublicHomepageHero>> GetPublicAsync(
        string languageCode,
        CancellationToken cancellationToken = default);
}
