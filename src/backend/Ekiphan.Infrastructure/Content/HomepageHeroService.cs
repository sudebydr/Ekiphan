using Ekiphan.Application.Catalog;
using Ekiphan.Application.Content;
using Ekiphan.Domain.Content;
using Ekiphan.Domain.Media;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Content;

internal sealed class HomepageHeroService(
    EkiphanDbContext dbContext,
    IPublicMediaUrlResolver mediaUrlResolver,
    TimeProvider timeProvider) : IHomepageHeroService
{
    public async Task<IReadOnlyList<AdminHomepageHero>> GetAdminAsync(
        CancellationToken cancellationToken = default) =>
        await ProjectAdmin(dbContext.HomepageHeroes.AsNoTracking()
                .OrderBy(item => item.SortOrder).ThenBy(item => item.Id))
            .ToListAsync(cancellationToken);

    public async Task<AdminHomepageHero> CreateAsync(
        SaveHomepageHeroCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        await ValidateMediaAsync(command, cancellationToken);
        var hero = new HomepageHero(Guid.NewGuid(), command.SortOrder);
        Apply(hero, command);
        dbContext.HomepageHeroes.Add(hero);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRequiredAsync(hero.Id, cancellationToken);
    }

    public async Task<AdminHomepageHero?> UpdateAsync(
        Guid id,
        SaveHomepageHeroCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        await ValidateMediaAsync(command, cancellationToken);
        var hero = await dbContext.HomepageHeroes.Include(item => item.Translations)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (hero is null) return null;
        Apply(hero, command);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRequiredAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<PublicHomepageHero>> GetPublicAsync(
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        var language = languageCode.Trim().ToLowerInvariant();
        if (language is not ("tr" or "en")) throw new ArgumentOutOfRangeException(nameof(languageCode));
        var now = timeProvider.GetUtcNow();
        var rows = await dbContext.HomepageHeroes.AsNoTracking()
            .Where(item => item.IsPublished &&
                (!item.StartsAt.HasValue || item.StartsAt <= now) &&
                (!item.EndsAt.HasValue || item.EndsAt > now))
            .SelectMany(item => item.Translations.Where(text => text.LanguageCode == language)
                .Select(text => new
                {
                    item.Id,
                    item.SortOrder,
                    text.Title,
                    text.Subtitle,
                    text.PrimaryCtaLabel,
                    text.PrimaryCtaUrl,
                    text.SecondaryCtaLabel,
                    text.SecondaryCtaUrl,
                    DesktopKey = dbContext.MediaAssets.Where(media =>
                        media.Id == item.DesktopMediaId &&
                        media.Status == MediaStatus.Active &&
                        media.AssetType == MediaAssetType.Image)
                        .Select(media => media.StorageKey).SingleOrDefault(),
                    MobileKey = dbContext.MediaAssets.Where(media =>
                        media.Id == item.MobileMediaId &&
                        media.Status == MediaStatus.Active &&
                        media.AssetType == MediaAssetType.Image)
                        .Select(media => media.StorageKey).SingleOrDefault(),
                }))
            .OrderBy(item => item.SortOrder).ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
        return rows.Select(item => new PublicHomepageHero(
            item.Id, item.Title, item.Subtitle, item.PrimaryCtaLabel,
            item.PrimaryCtaUrl, item.SecondaryCtaLabel, item.SecondaryCtaUrl,
            mediaUrlResolver.Resolve(item.DesktopKey),
            mediaUrlResolver.Resolve(item.MobileKey))).ToArray();
    }

    private static IQueryable<AdminHomepageHero> ProjectAdmin(
        IQueryable<HomepageHero> heroes) =>
        heroes.Select(item => new AdminHomepageHero(
            item.Id, item.DesktopMediaId, item.MobileMediaId, item.SortOrder,
            item.IsPublished, item.StartsAt, item.EndsAt,
            item.Translations.OrderBy(text => text.LanguageCode)
                .Select(text => new SaveHomepageHeroTranslation(
                    text.LanguageCode, text.Title, text.Subtitle,
                    text.PrimaryCtaLabel, text.PrimaryCtaUrl,
                    text.SecondaryCtaLabel, text.SecondaryCtaUrl)).ToArray()));

    private async Task<AdminHomepageHero> GetRequiredAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await ProjectAdmin(dbContext.HomepageHeroes.AsNoTracking()
                .Where(item => item.Id == id))
            .SingleAsync(cancellationToken);

    private static void Apply(HomepageHero hero, SaveHomepageHeroCommand command)
    {
        hero.Configure(command.DesktopMediaId, command.MobileMediaId,
            command.SortOrder, command.StartsAt, command.EndsAt);
        var languages = command.Translations.Select(item =>
            item.LanguageCode.Trim().ToLowerInvariant()).ToHashSet();
        foreach (var text in command.Translations)
            hero.SetTranslation(text.LanguageCode, text.Title, text.Subtitle,
                text.PrimaryCtaLabel, text.PrimaryCtaUrl,
                text.SecondaryCtaLabel, text.SecondaryCtaUrl);
        foreach (var language in hero.Translations.Select(item => item.LanguageCode)
            .Where(item => !languages.Contains(item)).ToArray())
            hero.RemoveTranslation(language);
        hero.SetPublished(command.IsPublished);
    }

    private async Task ValidateMediaAsync(
        SaveHomepageHeroCommand command,
        CancellationToken cancellationToken)
    {
        var ids = new[] { command.DesktopMediaId, command.MobileMediaId }
            .Where(item => item.HasValue).Select(item => item!.Value).Distinct().ToArray();
        if (ids.Length == 0) return;
        var count = await dbContext.MediaAssets.CountAsync(item =>
            ids.Contains(item.Id) && item.Status == MediaStatus.Active &&
            item.AssetType == MediaAssetType.Image, cancellationToken);
        if (count != ids.Length) throw new ArgumentException("Hero media must be active images.");
    }

    private static void Validate(SaveHomepageHeroCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Translations is null || command.Translations.Count is < 1 or > 2)
            throw new ArgumentException("One or two translations are required.");
        var languages = command.Translations.Select(item =>
            item.LanguageCode?.Trim().ToLowerInvariant()).ToArray();
        if (languages.Any(item => item is not ("tr" or "en")) ||
            languages.Distinct().Count() != languages.Length)
            throw new ArgumentException("Translations must use unique tr or en languages.");
    }
}
