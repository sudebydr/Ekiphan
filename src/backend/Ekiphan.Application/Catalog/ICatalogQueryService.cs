namespace Ekiphan.Application.Catalog;

public interface ICatalogQueryService
{
    Task<CatalogNavigation> GetNavigationAsync(
        string languageCode,
        CancellationToken cancellationToken = default);

    Task<CatalogPagedResult<CatalogProductSummary>> GetProductsAsync(
        CatalogProductQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CatalogFacet>> GetFacetsAsync(
        string languageCode,
        string categorySlug,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CatalogSitemapEntry>> GetSitemapEntriesAsync(
        string languageCode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CatalogBrandListItem>> GetBrandsAsync(
        string languageCode,
        CancellationToken cancellationToken = default);

    Task<CatalogBrandDetail?> GetBrandAsync(
        string languageCode,
        string slug,
        CancellationToken cancellationToken = default);

    Task<CatalogProductDetail?> GetProductAsync(
        string languageCode,
        string slug,
        CancellationToken cancellationToken = default);
}
