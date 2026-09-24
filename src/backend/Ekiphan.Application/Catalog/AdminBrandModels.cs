namespace Ekiphan.Application.Catalog;

public sealed record AdminBrandTranslationInput(
    string LanguageCode,
    string Description,
    string Slug);

public sealed record SaveAdminBrandCommand(
    string Name,
    string? WebsiteUrl,
    int SortOrder,
    bool IsPublished,
    IReadOnlyList<AdminBrandTranslationInput> Translations);

public sealed record AdminBrandSummary(
    Guid Id,
    string Name,
    string Slug,
    string? WebsiteUrl,
    bool IsPublished,
    int SortOrder);

public sealed record AdminBrandPage(
    IReadOnlyList<AdminBrandSummary> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record AdminBrandTranslation(
    string LanguageCode,
    string Description,
    string Slug);

public sealed record AdminBrandDetail(
    Guid Id,
    string Name,
    string? WebsiteUrl,
    bool IsPublished,
    int SortOrder,
    IReadOnlyList<AdminBrandTranslation> Translations,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public interface IAdminBrandService
{
    Task<AdminBrandPage> GetPageAsync(
        int page,
        int pageSize,
        string languageCode,
        string? search,
        CancellationToken cancellationToken = default);

    Task<AdminBrandDetail?> GetAsync(
        Guid brandId,
        CancellationToken cancellationToken = default);

    Task<AdminBrandDetail> CreateAsync(
        SaveAdminBrandCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminBrandDetail?> UpdateAsync(
        Guid brandId,
        SaveAdminBrandCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> ArchiveAsync(
        Guid brandId,
        CancellationToken cancellationToken = default);
}

public sealed class AdminBrandConflictException(string message)
    : Exception(message);
