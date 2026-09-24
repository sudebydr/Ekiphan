namespace Ekiphan.Application.Content;

public sealed record SavePressReleaseTranslation(
    string LanguageCode,
    string Title,
    string Summary,
    string? Body,
    string? Slug = null,
    string? MetaTitle = null,
    string? MetaDescription = null,
    string? CanonicalUrl = null,
    bool NoIndex = false,
    bool NoFollow = false,
    string? OpenGraphTitle = null,
    string? OpenGraphDescription = null,
    Guid? OpenGraphImageMediaId = null);

public sealed record SavePressReleaseCommand(
    Guid? CoverMediaId,
    Guid? AttachmentMediaId,
    DateTimeOffset PublishedAt,
    bool IsPublished,
    IReadOnlyList<SavePressReleaseTranslation> Translations);

public sealed record AdminPressRelease(
    Guid Id,
    Guid? CoverMediaId,
    Guid? AttachmentMediaId,
    DateTimeOffset PublishedAt,
    bool IsPublished,
    IReadOnlyList<SavePressReleaseTranslation> Translations);

public sealed record PublicPressRelease(
    Guid Id,
    string Title,
    string Summary,
    string? Body,
    DateTimeOffset PublishedAt,
    string? CoverImageUrl,
    string? CoverAltText,
    string? AttachmentUrl,
    string? AttachmentFileName,
    string? Slug = null,
    string? MetaTitle = null,
    string? MetaDescription = null,
    string? CanonicalUrl = null,
    bool NoIndex = false,
    bool NoFollow = false,
    string? OpenGraphTitle = null,
    string? OpenGraphDescription = null,
    string? OpenGraphImageUrl = null);

public interface IPressReleaseService
{
    Task<IReadOnlyList<AdminPressRelease>> GetAdminAsync(CancellationToken cancellationToken = default);
    Task<AdminPressRelease> CreateAsync(SavePressReleaseCommand command, CancellationToken cancellationToken = default);
    Task<AdminPressRelease?> UpdateAsync(Guid id, SavePressReleaseCommand command, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublicPressRelease>> GetPublicAsync(string languageCode, CancellationToken cancellationToken = default);
}
