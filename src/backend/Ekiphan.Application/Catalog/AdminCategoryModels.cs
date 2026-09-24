namespace Ekiphan.Application.Catalog;

public sealed record AdminSectionTranslationInput(
    string LanguageCode,
    string Name,
    string Slug);

public sealed record SaveAdminSectionCommand(
    string Code,
    int SortOrder,
    bool IsPublished,
    IReadOnlyList<AdminSectionTranslationInput> Translations);

public sealed record AdminSectionTranslation(
    string LanguageCode,
    string Name,
    string Slug);

public sealed record AdminSectionDetail(
    Guid Id,
    string Code,
    bool IsPublished,
    int SortOrder,
    IReadOnlyList<AdminSectionTranslation> Translations,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AdminCategoryTranslationInput(
    string LanguageCode,
    string Name,
    string Slug,
    string? Description,
    string? MetaTitle = null,
    string? MetaDescription = null,
    string? CanonicalUrl = null,
    bool NoIndex = false,
    bool NoFollow = false,
    string? OpenGraphTitle = null,
    string? OpenGraphDescription = null,
    Guid? OpenGraphImageMediaId = null);

public sealed record SaveAdminCategoryCommand(
    Guid ProductSectionId,
    Guid? ParentId,
    int SortOrder,
    bool IsPublished,
    IReadOnlyList<AdminCategoryTranslationInput> Translations);

public sealed record AdminCategoryTranslation(
    string LanguageCode,
    string Name,
    string Slug,
    string? Description,
    string? MetaTitle = null,
    string? MetaDescription = null,
    string? CanonicalUrl = null,
    bool NoIndex = false,
    bool NoFollow = false,
    string? OpenGraphTitle = null,
    string? OpenGraphDescription = null,
    Guid? OpenGraphImageMediaId = null,
    string? OpenGraphImageUrl = null);

public sealed record AdminCategoryDetail(
    Guid Id,
    Guid ProductSectionId,
    Guid? ParentId,
    bool IsPublished,
    int SortOrder,
    int ProductCount,
    IReadOnlyList<AdminCategoryTranslation> Translations,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AdminCatalogStructure(
    IReadOnlyList<AdminSectionDetail> Sections,
    IReadOnlyList<AdminCategoryDetail> Categories);

public interface IAdminCategoryService
{
    Task<AdminCatalogStructure> GetAsync(
        CancellationToken cancellationToken = default);

    Task<AdminSectionDetail> CreateSectionAsync(
        SaveAdminSectionCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminSectionDetail?> UpdateSectionAsync(
        Guid sectionId,
        SaveAdminSectionCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminCategoryDetail> CreateCategoryAsync(
        SaveAdminCategoryCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminCategoryDetail?> UpdateCategoryAsync(
        Guid categoryId,
        SaveAdminCategoryCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> ArchiveCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);
}

public sealed class AdminCategoryConflictException(string message)
    : Exception(message);
