using Ekiphan.Domain.Content;

namespace Ekiphan.Application.Content;

public sealed record SaveMenuTranslation(
    string LanguageCode,
    string Label);

public sealed record SaveMenuItemCommand(
    string Code,
    MenuLocation Location,
    string Url,
    bool IsExternal,
    bool OpenInNewTab,
    Guid? ParentId,
    int SortOrder,
    bool IsPublished,
    IReadOnlyList<SaveMenuTranslation> Translations);

public sealed record AdminMenuItem(
    Guid Id,
    string Code,
    MenuLocation Location,
    string Url,
    bool IsExternal,
    bool OpenInNewTab,
    Guid? ParentId,
    int SortOrder,
    bool IsPublished,
    IReadOnlyList<SaveMenuTranslation> Translations,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PublicMenuItem(
    Guid Id,
    Guid? ParentId,
    string Label,
    string Url,
    bool IsExternal,
    bool OpenInNewTab,
    int SortOrder);

public interface IMenuService
{
    Task<IReadOnlyList<AdminMenuItem>> GetAdminItemsAsync(
        CancellationToken cancellationToken = default);

    Task<AdminMenuItem> CreateAsync(
        SaveMenuItemCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminMenuItem?> UpdateAsync(
        Guid menuItemId,
        SaveMenuItemCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PublicMenuItem>> GetPublicItemsAsync(
        string languageCode,
        MenuLocation location,
        CancellationToken cancellationToken = default);
}

public sealed class MenuConflictException(string message)
    : Exception(message);

