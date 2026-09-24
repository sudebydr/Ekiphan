using Ekiphan.Application.Catalog;
using Ekiphan.Application.Content;
using Ekiphan.Domain.Content;
using Ekiphan.Domain.Media;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Content;

internal sealed class PressReleaseService(
    EkiphanDbContext dbContext,
    IPublicMediaUrlResolver mediaUrlResolver) : IPressReleaseService
{
    public async Task<IReadOnlyList<AdminPressRelease>> GetAdminAsync(
        CancellationToken cancellationToken = default) =>
        await ProjectAdmin(dbContext.PressReleases.AsNoTracking()
                .OrderByDescending(item => item.PublishedAt)
                .ThenBy(item => item.Id))
            .ToListAsync(cancellationToken);

    public async Task<AdminPressRelease> CreateAsync(
        SavePressReleaseCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        await ValidateMediaAsync(command, cancellationToken);
        var item = new PressRelease(Guid.NewGuid(), command.PublishedAt);
        Apply(item, command);
        dbContext.PressReleases.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRequiredAsync(item.Id, cancellationToken);
    }

    public async Task<AdminPressRelease?> UpdateAsync(
        Guid id,
        SavePressReleaseCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        await ValidateMediaAsync(command, cancellationToken);
        var item = await dbContext.PressReleases.Include(row => row.Translations)
            .SingleOrDefaultAsync(row => row.Id == id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        Apply(item, command);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRequiredAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<PublicPressRelease>> GetPublicAsync(
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        var language = languageCode.Trim().ToLowerInvariant();
        if (language is not ("tr" or "en"))
        {
            throw new ArgumentOutOfRangeException(nameof(languageCode));
        }

        var rows = await dbContext.PressReleases.AsNoTracking()
            .Where(item => item.IsPublished)
            .SelectMany(item => item.Translations
                .Where(text => text.LanguageCode == language)
                .Select(text => new
                {
                    item.Id,
                    text.Title,
                    text.Summary,
                    text.Body,
                    text.Slug,
                    text.MetaTitle,
                    text.MetaDescription,
                    text.CanonicalUrl,
                    text.NoIndex,
                    text.NoFollow,
                    text.OpenGraphTitle,
                    text.OpenGraphDescription,
                    item.PublishedAt,
                    CoverKey = dbContext.MediaAssets.Where(media =>
                            media.Id == item.CoverMediaId &&
                            media.Status == MediaStatus.Active &&
                            media.AssetType == MediaAssetType.Image)
                        .Select(media => media.StorageKey).SingleOrDefault(),
                    CoverAlt = dbContext.MediaAssets.Where(media => media.Id == item.CoverMediaId)
                        .SelectMany(media => media.Translations
                            .Where(mediaText => mediaText.LanguageCode == language)
                            .Select(mediaText => mediaText.AltText)).SingleOrDefault(),
                    AttachmentKey = dbContext.MediaAssets.Where(media =>
                            media.Id == item.AttachmentMediaId &&
                            media.Status == MediaStatus.Active &&
                            (media.AssetType == MediaAssetType.Pdf ||
                             media.AssetType == MediaAssetType.Document))
                        .Select(media => media.StorageKey).SingleOrDefault(),
                    AttachmentName = dbContext.MediaAssets.Where(media =>
                            media.Id == item.AttachmentMediaId)
                        .Select(media => media.OriginalFileName).SingleOrDefault(),
                    OpenGraphKey = dbContext.MediaAssets.Where(media =>
                            media.Id == text.OpenGraphImageMediaId &&
                            media.Status == MediaStatus.Active &&
                            media.AssetType == MediaAssetType.Image)
                        .Select(media => media.StorageKey).SingleOrDefault(),
                }))
            .OrderByDescending(item => item.PublishedAt)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

        return rows.Select(item => new PublicPressRelease(
            item.Id,
            item.Title,
            item.Summary,
            item.Body,
            item.PublishedAt,
            mediaUrlResolver.Resolve(item.CoverKey),
            item.CoverAlt,
            mediaUrlResolver.Resolve(item.AttachmentKey),
            item.AttachmentName,
            item.Slug,
            item.MetaTitle,
            item.MetaDescription,
            item.CanonicalUrl,
            item.NoIndex,
            item.NoFollow,
            item.OpenGraphTitle,
            item.OpenGraphDescription,
            mediaUrlResolver.Resolve(item.OpenGraphKey))).ToArray();
    }

    private static IQueryable<AdminPressRelease> ProjectAdmin(
        IQueryable<PressRelease> releases) =>
        releases.Select(item => new AdminPressRelease(
            item.Id,
            item.CoverMediaId,
            item.AttachmentMediaId,
            item.PublishedAt,
            item.IsPublished,
            item.Translations.OrderBy(text => text.LanguageCode)
                .Select(text => new SavePressReleaseTranslation(
                    text.LanguageCode, text.Title, text.Summary, text.Body,
                    text.Slug, text.MetaTitle, text.MetaDescription,
                    text.CanonicalUrl, text.NoIndex, text.NoFollow,
                    text.OpenGraphTitle, text.OpenGraphDescription,
                    text.OpenGraphImageMediaId)).ToArray()));

    private async Task<AdminPressRelease> GetRequiredAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await ProjectAdmin(dbContext.PressReleases.AsNoTracking()
                .Where(item => item.Id == id))
            .SingleAsync(cancellationToken);

    private static void Apply(PressRelease item, SavePressReleaseCommand command)
    {
        item.Configure(command.CoverMediaId, command.AttachmentMediaId, command.PublishedAt);
        var languages = command.Translations.Select(text =>
            text.LanguageCode.Trim().ToLowerInvariant()).ToHashSet();
        foreach (var text in command.Translations)
        {
            item.SetTranslation(
                text.LanguageCode, text.Title, text.Summary, text.Body,
                text.Slug, text.MetaTitle, text.MetaDescription,
                text.CanonicalUrl, text.NoIndex, text.NoFollow,
                text.OpenGraphTitle, text.OpenGraphDescription,
                text.OpenGraphImageMediaId);
        }

        foreach (var language in item.Translations.Select(text => text.LanguageCode)
            .Where(language => !languages.Contains(language)).ToArray())
        {
            item.RemoveTranslation(language);
        }

        item.SetPublished(command.IsPublished);
    }

    private async Task ValidateMediaAsync(
        SavePressReleaseCommand command,
        CancellationToken cancellationToken)
    {
        if (command.CoverMediaId.HasValue)
        {
            var validCover = await dbContext.MediaAssets.AnyAsync(media =>
                media.Id == command.CoverMediaId &&
                media.Status == MediaStatus.Active &&
                media.AssetType == MediaAssetType.Image, cancellationToken);
            if (!validCover)
            {
                throw new ArgumentException("Press release cover must be an active image.");
            }
        }

        if (command.AttachmentMediaId.HasValue)
        {
            var validAttachment = await dbContext.MediaAssets.AnyAsync(media =>
                media.Id == command.AttachmentMediaId &&
                media.Status == MediaStatus.Active &&
                (media.AssetType == MediaAssetType.Pdf ||
                 media.AssetType == MediaAssetType.Document), cancellationToken);
            if (!validAttachment)
            {
                throw new ArgumentException("Press release attachment must be an active PDF or document.");
            }
        }

        var ogIds = command.Translations
            .Where(item => item.OpenGraphImageMediaId.HasValue)
            .Select(item => item.OpenGraphImageMediaId!.Value)
            .Distinct()
            .ToArray();
        if (ogIds.Length > 0)
        {
            var count = await dbContext.MediaAssets.CountAsync(media =>
                ogIds.Contains(media.Id) &&
                media.Status == MediaStatus.Active &&
                media.AssetType == MediaAssetType.Image, cancellationToken);
            if (count != ogIds.Length)
            {
                throw new ArgumentException(
                    "Open Graph images must reference active image media.");
            }
        }
    }

    private static void Validate(SavePressReleaseCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.PublishedAt == default)
        {
            throw new ArgumentException("Publication date is required.");
        }
        if (command.Translations is null || command.Translations.Count is < 1 or > 2)
        {
            throw new ArgumentException("One or two translations are required.");
        }
        var languages = command.Translations.Select(item =>
            item.LanguageCode?.Trim().ToLowerInvariant()).ToArray();
        if (languages.Any(item => item is not ("tr" or "en")) ||
            languages.Distinct().Count() != languages.Length)
        {
            throw new ArgumentException("Translations must use unique tr or en languages.");
        }
    }
}
