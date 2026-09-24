namespace Ekiphan.Application.Seo;

public enum SeoContentType { Product, Category, ContentPage, PressRelease }

public sealed record AdminSeoAccessScope(bool Catalog, bool Content);

public sealed record AdminSeoQuery(
    SeoContentType? ContentType = null,
    string? Language = null,
    bool? Published = null,
    bool? MissingMetaTitle = null,
    bool? MissingMetaDescription = null,
    bool? MissingOpenGraphImage = null,
    bool? DuplicateSlug = null,
    bool? DuplicateMetaTitle = null,
    bool? NoIndex = null,
    string? Search = null,
    string Sort = "updated-desc",
    int Page = 1,
    int PageSize = 25);

public sealed record AdminSeoRow(
    SeoContentType ContentType,
    Guid ContentId,
    string ContentName,
    string Language,
    string Slug,
    string? MetaTitle,
    int Score,
    bool NoIndex,
    bool Published,
    DateTimeOffset UpdatedAt,
    string EditUrl);

public sealed record AdminSeoPage(
    IReadOnlyList<AdminSeoRow> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record AdminSeoHealth(
    int TotalIndexable,
    int MissingMetaTitle,
    int MissingMetaDescription,
    int MissingOpenGraphImage,
    int DuplicateSlugs,
    int DuplicateMetaTitles,
    int NoIndex,
    int MissingEnglish,
    IReadOnlyList<AdminSeoRow> RecentlyUpdated);

public sealed record AdminSeoTranslation(
    string Language,
    string ContentName,
    string Slug,
    string? MetaTitle,
    string? MetaDescription,
    string? CanonicalUrl,
    string? OpenGraphTitle,
    string? OpenGraphDescription,
    Guid? OpenGraphImageMediaId,
    string? OpenGraphImageUrl,
    bool NoIndex,
    bool NoFollow);

public sealed record AdminSeoDetail(
    SeoContentType ContentType,
    Guid ContentId,
    bool Published,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<AdminSeoTranslation> Translations);

public sealed record UpdateAdminSeoCommand(
    IReadOnlyList<AdminSeoTranslation> Translations);

public sealed record AdminSeoDuplicate(
    SeoContentType ContentType,
    string Language,
    string Field,
    string Value,
    int Count);

public sealed record SlugAvailability(bool Available);

public interface IAdminSeoService
{
    Task<AdminSeoPage> GetPageAsync(AdminSeoQuery query, AdminSeoAccessScope scope, CancellationToken cancellationToken = default);
    Task<AdminSeoHealth> GetHealthAsync(AdminSeoAccessScope scope, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminSeoDuplicate>> GetDuplicatesAsync(AdminSeoAccessScope scope, CancellationToken cancellationToken = default);
    Task<AdminSeoDetail?> GetDetailAsync(SeoContentType type, Guid id, AdminSeoAccessScope scope, CancellationToken cancellationToken = default);
    Task<AdminSeoDetail?> UpdateAsync(SeoContentType type, Guid id, UpdateAdminSeoCommand command, AdminSeoAccessScope scope, CancellationToken cancellationToken = default);
    Task<SlugAvailability> IsSlugAvailableAsync(SeoContentType type, Guid? id, string language, string slug, AdminSeoAccessScope scope, CancellationToken cancellationToken = default);
}

public sealed class AdminSeoConflictException(string message) : Exception(message);
