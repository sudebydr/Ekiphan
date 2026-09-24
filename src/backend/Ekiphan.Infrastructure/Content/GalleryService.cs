using Ekiphan.Application.Catalog;
using Ekiphan.Application.Content;
using Ekiphan.Domain.Content;
using Ekiphan.Domain.Media;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Content;

internal sealed class GalleryService(
    EkiphanDbContext dbContext,
    IPublicMediaUrlResolver mediaUrlResolver) : IGalleryService
{
    public async Task<IReadOnlyList<AdminGalleryItem>> GetAdminAsync(
        CancellationToken cancellationToken = default) =>
        await ProjectAdmin(dbContext.GalleryItems.AsNoTracking()
                .OrderBy(item => item.SortOrder).ThenBy(item => item.Id))
            .ToListAsync(cancellationToken);

    public async Task<AdminGalleryItem> CreateAsync(
        SaveGalleryItemCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        await ValidateMediaAsync(command.MediaAssetId, cancellationToken);
        var item = new GalleryItem(Guid.NewGuid(), command.MediaAssetId, command.SortOrder);
        Apply(item, command);
        dbContext.GalleryItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRequiredAsync(item.Id, cancellationToken);
    }

    public async Task<AdminGalleryItem?> UpdateAsync(
        Guid id,
        SaveGalleryItemCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        await ValidateMediaAsync(command.MediaAssetId, cancellationToken);
        var item = await dbContext.GalleryItems.Include(row => row.Translations)
            .SingleOrDefaultAsync(row => row.Id == id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        Apply(item, command);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRequiredAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<PublicGalleryItem>> GetPublicAsync(
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        var language = languageCode.Trim().ToLowerInvariant();
        if (language is not ("tr" or "en"))
        {
            throw new ArgumentOutOfRangeException(nameof(languageCode));
        }

        var rows = await dbContext.GalleryItems.AsNoTracking()
            .Where(item => item.IsPublished)
            .SelectMany(item => item.Translations
                .Where(text => text.LanguageCode == language)
                .Select(text => new
                {
                    item.Id,
                    item.SortOrder,
                    text.Title,
                    text.Caption,
                    StorageKey = dbContext.MediaAssets
                        .Where(media => media.Id == item.MediaAssetId &&
                            media.Status == MediaStatus.Active &&
                            media.AssetType == MediaAssetType.Image)
                        .Select(media => media.StorageKey)
                        .SingleOrDefault(),
                    AltText = dbContext.MediaAssets
                        .Where(media => media.Id == item.MediaAssetId)
                        .SelectMany(media => media.Translations
                            .Where(mediaText => mediaText.LanguageCode == language)
                            .Select(mediaText => mediaText.AltText))
                        .SingleOrDefault(),
                }))
            .Where(item => item.StorageKey != null && item.AltText != null)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

        return rows.Select(item => new PublicGalleryItem(
            item.Id,
            item.Title,
            item.Caption,
            mediaUrlResolver.Resolve(item.StorageKey)!,
            item.AltText!)).ToArray();
    }

    private static IQueryable<AdminGalleryItem> ProjectAdmin(
        IQueryable<GalleryItem> items) =>
        items.Select(item => new AdminGalleryItem(
            item.Id,
            item.MediaAssetId,
            item.SortOrder,
            item.IsPublished,
            item.Translations.OrderBy(text => text.LanguageCode)
                .Select(text => new SaveGalleryTranslation(
                    text.LanguageCode,
                    text.Title,
                    text.Caption)).ToArray()));

    private async Task<AdminGalleryItem> GetRequiredAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await ProjectAdmin(dbContext.GalleryItems.AsNoTracking()
                .Where(item => item.Id == id))
            .SingleAsync(cancellationToken);

    private static void Apply(GalleryItem item, SaveGalleryItemCommand command)
    {
        item.Configure(command.MediaAssetId, command.SortOrder);
        var languages = command.Translations
            .Select(text => text.LanguageCode.Trim().ToLowerInvariant())
            .ToHashSet();
        foreach (var text in command.Translations)
        {
            item.SetTranslation(text.LanguageCode, text.Title, text.Caption);
        }

        foreach (var language in item.Translations.Select(text => text.LanguageCode)
            .Where(language => !languages.Contains(language)).ToArray())
        {
            item.RemoveTranslation(language);
        }

        item.SetPublished(command.IsPublished);
    }

    private async Task ValidateMediaAsync(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        var valid = await dbContext.MediaAssets.AnyAsync(media =>
            media.Id == mediaAssetId &&
            media.Status == MediaStatus.Active &&
            media.AssetType == MediaAssetType.Image,
            cancellationToken);
        if (!valid)
        {
            throw new ArgumentException("Gallery media must be an active image.");
        }
    }

    private static void Validate(SaveGalleryItemCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Translations is null || command.Translations.Count is < 1 or > 2)
        {
            throw new ArgumentException("One or two translations are required.");
        }

        var languages = command.Translations
            .Select(item => item.LanguageCode?.Trim().ToLowerInvariant()).ToArray();
        if (languages.Any(item => item is not ("tr" or "en")) ||
            languages.Distinct().Count() != languages.Length)
        {
            throw new ArgumentException("Translations must use unique tr or en languages.");
        }
    }
}
