namespace Ekiphan.Application.Content;

public sealed record SaveGalleryTranslation(
    string LanguageCode,
    string Title,
    string? Caption);

public sealed record SaveGalleryItemCommand(
    Guid MediaAssetId,
    int SortOrder,
    bool IsPublished,
    IReadOnlyList<SaveGalleryTranslation> Translations);

public sealed record AdminGalleryItem(
    Guid Id,
    Guid MediaAssetId,
    int SortOrder,
    bool IsPublished,
    IReadOnlyList<SaveGalleryTranslation> Translations);

public sealed record PublicGalleryItem(
    Guid Id,
    string Title,
    string? Caption,
    string ImageUrl,
    string AltText);

public interface IGalleryService
{
    Task<IReadOnlyList<AdminGalleryItem>> GetAdminAsync(
        CancellationToken cancellationToken = default);
    Task<AdminGalleryItem> CreateAsync(
        SaveGalleryItemCommand command,
        CancellationToken cancellationToken = default);
    Task<AdminGalleryItem?> UpdateAsync(
        Guid id,
        SaveGalleryItemCommand command,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublicGalleryItem>> GetPublicAsync(
        string languageCode,
        CancellationToken cancellationToken = default);
}
