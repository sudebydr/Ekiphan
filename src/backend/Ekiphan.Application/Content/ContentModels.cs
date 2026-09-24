using Ekiphan.Domain.Content;

namespace Ekiphan.Application.Content;

public sealed record SaveContentTranslation(
    string LanguageCode,
    string Title,
    string Slug,
    string? Summary,
    string Body,
    string? MetaTitle,
    string? MetaDescription,
    string? CanonicalUrl,
    bool NoIndex,
    bool NoFollow,
    string? OpenGraphTitle = null,
    string? OpenGraphDescription = null,
    Guid? OpenGraphImageMediaId = null);

public sealed record SaveContentPageCommand(
    string Code,
    ContentStatus Status,
    IReadOnlyList<SaveContentTranslation> Translations);

public sealed record AdminContentPage(
    Guid Id,
    string Code,
    ContentStatus Status,
    DateTimeOffset? PublishedAt,
    IReadOnlyList<SaveContentTranslation> Translations,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PublicContentPage(
    Guid Id,
    string Code,
    string LanguageCode,
    string Title,
    string Slug,
    string? Summary,
    string Body,
    string? MetaTitle,
    string? MetaDescription,
    string? CanonicalUrl,
    bool NoIndex,
    bool NoFollow,
    DateTimeOffset UpdatedAt,
    string? OpenGraphTitle = null,
    string? OpenGraphDescription = null,
    Guid? OpenGraphImageMediaId = null,
    string? OpenGraphImageUrl = null,
    IReadOnlyList<PublicSeoAlternate>? Alternates = null);

public sealed record PublicSeoAlternate(string LanguageCode, string Slug);

public sealed record PublicContentSitemapEntry(
    string Slug,
    DateTimeOffset UpdatedAt);

public interface IContentPageService
{
    Task<IReadOnlyList<AdminContentPage>> GetAdminPagesAsync(
        CancellationToken cancellationToken = default);

    Task<AdminContentPage> CreateAsync(
        SaveContentPageCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminContentPage?> UpdateAsync(
        Guid pageId,
        SaveContentPageCommand command,
        CancellationToken cancellationToken = default);

    Task<PublicContentPage?> GetPublishedAsync(
        string languageCode,
        string slug,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PublicContentSitemapEntry>> GetSitemapAsync(
        string languageCode,
        CancellationToken cancellationToken = default);
}

public sealed class ContentPageConflictException(string message)
    : Exception(message);
