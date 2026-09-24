using Ekiphan.Application.Content;
using Ekiphan.Domain.Content;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Content;

internal sealed class MenuService(EkiphanDbContext dbContext)
    : IMenuService
{
    public async Task<IReadOnlyList<AdminMenuItem>> GetAdminItemsAsync(
        CancellationToken cancellationToken = default) =>
        await ProjectAdmin(
                dbContext.MenuItems
                    .AsNoTracking()
                    .OrderBy(item => item.Location)
                    .ThenBy(item => item.SortOrder)
                    .ThenBy(item => item.Code))
            .ToListAsync(cancellationToken);

    public async Task<AdminMenuItem> CreateAsync(
        SaveMenuItemCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        await ValidateParentAsync(
            null,
            command.ParentId,
            command.Location,
            command.IsPublished,
            cancellationToken);
        var item = new MenuItem(
            Guid.NewGuid(),
            command.Code,
            command.Location,
            command.Url,
            command.IsExternal,
            command.OpenInNewTab,
            command.ParentId,
            command.SortOrder);
        Apply(item, command);
        dbContext.MenuItems.Add(item);
        await SaveAsync(cancellationToken);
        return await GetRequiredAsync(item.Id, cancellationToken);
    }

    public async Task<AdminMenuItem?> UpdateAsync(
        Guid menuItemId,
        SaveMenuItemCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        var item = await dbContext.MenuItems
            .Include(value => value.Translations)
            .SingleOrDefaultAsync(
                value => value.Id == menuItemId,
                cancellationToken);
        if (item is null)
        {
            return null;
        }

        await ValidateParentAsync(
            menuItemId,
            command.ParentId,
            command.Location,
            command.IsPublished,
            cancellationToken);
        item.Configure(
            command.Code,
            command.Location,
            command.Url,
            command.IsExternal,
            command.OpenInNewTab,
            command.ParentId,
            command.SortOrder);
        Apply(item, command);
        await SaveAsync(cancellationToken);
        return await GetRequiredAsync(item.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<PublicMenuItem>> GetPublicItemsAsync(
        string languageCode,
        MenuLocation location,
        CancellationToken cancellationToken = default)
    {
        var language = languageCode.Trim().ToLowerInvariant();
        var items = await dbContext.MenuItems
            .AsNoTracking()
            .Where(item =>
                item.Location == location &&
                item.IsPublished)
            .SelectMany(
                item => item.Translations.Where(
                    text => text.LanguageCode == language),
                (item, text) => new
                {
                    item.Id,
                    item.ParentId,
                    text.Label,
                    item.Url,
                    item.IsExternal,
                    item.OpenInNewTab,
                    item.SortOrder,
                })
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Label)
            .Select(item => new PublicMenuItem(
                item.Id,
                item.ParentId,
                item.Label,
                item.Url,
                item.IsExternal,
                item.OpenInNewTab,
                item.SortOrder))
            .ToListAsync(cancellationToken);

        var visibleIds = items.Select(item => item.Id).ToHashSet();
        var changed = true;
        while (changed)
        {
            var removed = items.RemoveAll(
                item => item.ParentId.HasValue &&
                    !visibleIds.Contains(item.ParentId.Value));
            changed = removed > 0;
            if (changed)
            {
                visibleIds = items.Select(item => item.Id).ToHashSet();
            }
        }

        return items;
    }

    private static IQueryable<AdminMenuItem> ProjectAdmin(
        IQueryable<MenuItem> source) =>
        source
            .Select(item => new AdminMenuItem(
                item.Id,
                item.Code,
                item.Location,
                item.Url,
                item.IsExternal,
                item.OpenInNewTab,
                item.ParentId,
                item.SortOrder,
                item.IsPublished,
                item.Translations
                    .OrderBy(text => text.LanguageCode)
                    .Select(text => new SaveMenuTranslation(
                        text.LanguageCode,
                        text.Label))
                    .ToArray(),
                item.CreatedAt,
                item.UpdatedAt));

    private async Task<AdminMenuItem> GetRequiredAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await ProjectAdmin(
                dbContext.MenuItems
                    .AsNoTracking()
                    .Where(item => item.Id == id))
            .SingleAsync(cancellationToken);

    private async Task ValidateParentAsync(
        Guid? itemId,
        Guid? parentId,
        MenuLocation location,
        bool isPublished,
        CancellationToken cancellationToken)
    {
        if (!parentId.HasValue)
        {
            return;
        }

        var items = await dbContext.MenuItems
            .AsNoTracking()
            .Select(item => new
            {
                item.Id,
                item.ParentId,
                item.Location,
                item.IsPublished,
            })
            .ToListAsync(cancellationToken);
        var byId = items.ToDictionary(item => item.Id);
        if (!byId.TryGetValue(parentId.Value, out var parent) ||
            parent.Location != location)
        {
            throw new ArgumentException(
                "Parent menu item must exist in the same location.");
        }

        if (isPublished && !parent.IsPublished)
        {
            throw new ArgumentException(
                "A published child requires a published parent.");
        }

        var current = parent;
        var depth = 1;
        while (current.ParentId.HasValue)
        {
            if (current.Id == itemId)
            {
                throw new ArgumentException(
                    "Menu hierarchy cannot contain a cycle.");
            }

            if (!byId.TryGetValue(current.ParentId.Value, out current))
            {
                throw new ArgumentException(
                    "Menu hierarchy contains a missing parent.");
            }

            depth++;
            if (depth > 2)
            {
                throw new ArgumentException(
                    "Menu hierarchy supports up to three levels.");
            }
        }

        if (current.Id == itemId)
        {
            throw new ArgumentException(
                "Menu hierarchy cannot contain a cycle.");
        }
    }

    private static void Apply(MenuItem item, SaveMenuItemCommand command)
    {
        var languages = command.Translations
            .Select(value => value.LanguageCode.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
        foreach (var text in command.Translations)
        {
            item.SetTranslation(text.LanguageCode, text.Label);
        }

        foreach (var language in item.Translations
            .Select(value => value.LanguageCode)
            .Where(value => !languages.Contains(value))
            .ToArray())
        {
            item.RemoveTranslation(language);
        }

        item.SetPublished(command.IsPublished);
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new MenuConflictException(
                "The menu code is already in use.");
        }
    }

    private static void Validate(SaveMenuItemCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!Enum.IsDefined(command.Location))
        {
            throw new ArgumentException("Menu location is not supported.");
        }

        if (command.Translations is null ||
            command.Translations.Count is < 1 or > 2)
        {
            throw new ArgumentException(
                "One or two menu translations are required.");
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
