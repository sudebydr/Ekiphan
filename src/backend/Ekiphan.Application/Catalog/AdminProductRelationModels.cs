using Ekiphan.Domain.Catalog;

namespace Ekiphan.Application.Catalog;

public sealed record AdminCatalogProductSummary(
    Guid Id,
    string SKU,
    string Name,
    bool IsPublished,
    Guid? BrandId,
    string? BrandName,
    Guid? PrimaryCategoryId,
    string? PrimaryCategoryName,
    bool MissingGalleryImage,
    bool MissingEnglishContent,
    DateTimeOffset UpdatedAt);

public sealed record AdminCatalogProductPage(
    IReadOnlyList<AdminCatalogProductSummary> Items,
    int Page,
    int PageSize,
    int TotalCount);

public enum AdminProductSortBy
{
    Name = 1,
    SKU = 2,
    UpdatedAt = 3,
    CreatedAt = 4,
}

public enum AdminSortDirection
{
    Ascending = 1,
    Descending = 2,
}

public sealed record AdminProductListQuery(
    int Page,
    int PageSize,
    string LanguageCode,
    string? Search,
    Guid? CategoryId,
    Guid? BrandId,
    bool? IsPublished,
    bool MissingGalleryImage,
    bool MissingEnglishContent,
    AdminProductSortBy SortBy,
    AdminSortDirection SortDirection);

public sealed record AdminProductRelation(
    Guid Id,
    Guid SourceProductId,
    Guid TargetProductId,
    Guid RelatedProductId,
    string RelatedSKU,
    string RelatedName,
    ProductRelationType RelationType,
    bool IsBidirectional,
    bool IsIncoming,
    int SortOrder);

public sealed record CreateAdminProductRelationCommand(
    Guid SourceProductId,
    Guid TargetProductId,
    ProductRelationType RelationType,
    bool IsBidirectional,
    int SortOrder);

public interface IAdminProductRelationService
{
    Task<AdminCatalogProductPage> GetProductsAsync(
        AdminProductListQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminProductRelation>> GetRelationsAsync(
        Guid productId,
        string languageCode,
        CancellationToken cancellationToken = default);

    Task<AdminProductRelation> CreateAsync(
        CreateAdminProductRelationCommand command,
        string languageCode,
        CancellationToken cancellationToken = default);

    Task DeactivateAsync(
        Guid relationId,
        CancellationToken cancellationToken = default);
}

public sealed class CatalogProductNotFoundException : Exception
{
    public CatalogProductNotFoundException()
        : base("The selected product does not exist.")
    {
    }
}

public sealed class ProductRelationNotFoundException : Exception
{
    public ProductRelationNotFoundException()
        : base("The product relation does not exist.")
    {
    }
}

public sealed class ProductRelationConflictException(string message)
    : Exception(message);
