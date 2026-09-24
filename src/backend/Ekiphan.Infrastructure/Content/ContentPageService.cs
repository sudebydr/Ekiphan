using Ekiphan.Application.Catalog;
using Ekiphan.Application.Content;
using Ekiphan.Domain.Content;
using Ekiphan.Domain.Media;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Content;

internal sealed class ContentPageService(
    EkiphanDbContext dbContext,
    TimeProvider timeProvider,
    IPublicMediaUrlResolver mediaUrlResolver)
    : IContentPageService
{
    public async Task<IReadOnlyList<AdminContentPage>> GetAdminPagesAsync(
        CancellationToken cancellationToken = default) =>
        await ProjectAdmin(dbContext.ContentPages
                .AsNoTracking()
                .OrderBy(item => item.Code))
            .ToListAsync(cancellationToken);

    public async Task<AdminContentPage> CreateAsync(
        SaveContentPageCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        await ValidateMediaAsync(command, cancellationToken);
        var page = new ContentPage(Guid.NewGuid(), command.Code);
        Apply(page, command);
        dbContext.ContentPages.Add(page);
        await SaveAsync(cancellationToken);
        return await GetRequiredAsync(page.Id, cancellationToken);
    }

    public async Task<AdminContentPage?> UpdateAsync(
        Guid pageId,
        SaveContentPageCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        await ValidateMediaAsync(command, cancellationToken);
        var page = await dbContext.ContentPages
            .Include(item => item.Translations)
            .SingleOrDefaultAsync(item => item.Id == pageId, cancellationToken);
        if (page is null)
        {
            return null;
        }

        page.UpdateCode(command.Code);
        Apply(page, command);
        await SaveAsync(cancellationToken);
        return await GetRequiredAsync(page.Id, cancellationToken);
    }

    public async Task<PublicContentPage?> GetPublishedAsync(
        string languageCode,
        string slug,
        CancellationToken cancellationToken = default)
    {
        var language = languageCode.Trim().ToLowerInvariant();
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var row = await dbContext.ContentPages
            .AsNoTracking()
            .Where(item => item.Status == ContentStatus.Published)
            .SelectMany(
                item => item.Translations
                    .Where(text =>
                        text.LanguageCode == language &&
                        text.Slug == normalizedSlug)
                    .Select(text => new
                    {
                        item.Id,
                        item.Code,
                        text.LanguageCode,
                        text.Title,
                        text.Slug,
                        text.Summary,
                        text.Body,
                        text.MetaTitle,
                        text.MetaDescription,
                        text.CanonicalUrl,
                        text.NoIndex,
                        text.NoFollow,
                        item.UpdatedAt,
                        text.OpenGraphTitle,
                        text.OpenGraphDescription,
                        text.OpenGraphImageMediaId,
                        OpenGraphImageKey = dbContext.MediaAssets
                            .Where(media => media.Id == text.OpenGraphImageMediaId &&
                                media.Status == MediaStatus.Active &&
                                media.AssetType == MediaAssetType.Image)
                            .Select(media => media.StorageKey)
                            .SingleOrDefault(),
                        Alternates = item.Translations
                            .OrderBy(alternate => alternate.LanguageCode)
                            .Select(alternate => new PublicSeoAlternate(
                                alternate.LanguageCode,
                                alternate.Slug))
                            .ToArray(),
                    }))
            .SingleOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new PublicContentPage(
                row.Id, row.Code, row.LanguageCode, row.Title, row.Slug,
                row.Summary, row.Body, row.MetaTitle, row.MetaDescription,
                row.CanonicalUrl, row.NoIndex, row.NoFollow, row.UpdatedAt,
                row.OpenGraphTitle, row.OpenGraphDescription,
                row.OpenGraphImageMediaId,
                mediaUrlResolver.Resolve(row.OpenGraphImageKey),
                row.Alternates);
    }

    public async Task<IReadOnlyList<PublicContentSitemapEntry>> GetSitemapAsync(
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        var language = languageCode.Trim().ToLowerInvariant();
        return await dbContext.ContentPages
            .AsNoTracking()
            .Where(item => item.Status == ContentStatus.Published)
            .SelectMany(item => item.Translations
                .Where(text =>
                    text.LanguageCode == language &&
                    !text.NoIndex)
                .Select(text => new PublicContentSitemapEntry(
                    text.Slug,
                    item.UpdatedAt)))
            .OrderBy(item => item.Slug)
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<AdminContentPage> ProjectAdmin(
        IQueryable<ContentPage> pages) =>
        pages.Select(item => new AdminContentPage(
                item.Id,
                item.Code,
                item.Status,
                item.PublishedAt,
                item.Translations
                    .OrderBy(text => text.LanguageCode)
                    .Select(text => new SaveContentTranslation(
                        text.LanguageCode,
                        text.Title,
                        text.Slug,
                        text.Summary,
                        text.Body,
                        text.MetaTitle,
                        text.MetaDescription,
                        text.CanonicalUrl,
                        text.NoIndex,
                        text.NoFollow,
                        text.OpenGraphTitle,
                        text.OpenGraphDescription,
                        text.OpenGraphImageMediaId))
                    .ToArray(),
                item.CreatedAt,
                item.UpdatedAt));

    private async Task<AdminContentPage> GetRequiredAsync(
        Guid pageId,
        CancellationToken cancellationToken) =>
        await ProjectAdmin(dbContext.ContentPages
                .AsNoTracking()
                .Where(item => item.Id == pageId))
            .SingleAsync(cancellationToken);

    private void Apply(ContentPage page, SaveContentPageCommand command)
    {
        var languages = command.Translations
            .Select(item => item.LanguageCode.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
        foreach (var text in command.Translations)
        {
            page.SetTranslation(
                text.LanguageCode,
                text.Title,
                text.Slug,
                text.Summary,
                text.Body,
                text.MetaTitle,
                text.MetaDescription,
                text.CanonicalUrl,
                text.NoIndex,
                text.NoFollow,
                text.OpenGraphTitle,
                text.OpenGraphDescription,
                text.OpenGraphImageMediaId);
        }

        foreach (var language in page.Translations
            .Select(item => item.LanguageCode)
            .Where(item => !languages.Contains(item))
            .ToArray())
        {
            page.RemoveTranslation(language);
        }

        page.SetStatus(command.Status, timeProvider.GetUtcNow());
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ContentPageConflictException(
                "The content code or translated slug is already in use.");
        }
    }

    private async Task ValidateMediaAsync(
        SaveContentPageCommand command,
        CancellationToken cancellationToken)
    {
        var ids = command.Translations
            .Where(item => item.OpenGraphImageMediaId.HasValue)
            .Select(item => item.OpenGraphImageMediaId!.Value)
            .Distinct()
            .ToArray();
        if (ids.Length == 0)
        {
            return;
        }

        var validCount = await dbContext.MediaAssets.CountAsync(media =>
            ids.Contains(media.Id) && media.Status == MediaStatus.Active &&
            media.AssetType == MediaAssetType.Image, cancellationToken);
        if (validCount != ids.Length)
        {
            throw new ArgumentException(
                "Open Graph images must reference active image media.");
        }
    }

    private static void Validate(SaveContentPageCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!Enum.IsDefined(command.Status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                "Content status is not supported.");
        }

        if (command.Translations is null ||
            command.Translations.Count is < 1 or > 2)
        {
            throw new ArgumentException(
                "One or two translations are required.");
        }

        var languages = command.Translations
            .Select(item => item.LanguageCode?.Trim().ToLowerInvariant())
            .ToArray();
        if (languages.Any(item => item is not ("tr" or "en")) ||
            languages.Distinct(StringComparer.Ordinal).Count() !=
                languages.Length)
        {
            throw new ArgumentException(
                "Translations must use unique tr or en language codes.");
        }
    }
}
