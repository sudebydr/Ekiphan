using Ekiphan.Domain.Catalog;

namespace Ekiphan.Application.Catalog;

public sealed record CatalogProductQuery(
    string LanguageCode,
    int Page = 1,
    int PageSize = 24,
    string? Search = null,
    string? SectionSlug = null,
    string? CategorySlug = null,
    string? BrandSlug = null,
    string? TagSlug = null,
    CatalogProductSort Sort = CatalogProductSort.Name,
    IReadOnlyList<CatalogAttributeFilter>? AttributeFilters = null,
    string? UsageArea = null);

public sealed record CatalogAttributeFilter(Guid AttributeId, string Value);

public sealed record CatalogFacetOption(string Value, string Label);

public sealed record CatalogFacet(
    Guid AttributeId,
    string Code,
    string Name,
    AttributeDataType DataType,
    string? UnitSymbol,
    decimal? Minimum,
    decimal? Maximum,
    IReadOnlyList<CatalogFacetOption> Options);

public enum CatalogProductSort
{
    Name = 1,
    NameDescending = 2,
    Newest = 3,
}

public sealed record CatalogPagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

public static class CatalogSitemapLimits
{
    public const int MaximumProductUrls = 49_998;
}

public sealed record CatalogSitemapEntry(
    string Slug,
    DateTimeOffset UpdatedAt,
    string Kind = "product");

public sealed record CatalogProductSummary(
    Guid Id,
    string SKU,
    string Name,
    string Slug,
    string? ShortDescription,
    CatalogBrandSummary? Brand,
    CatalogCategorySummary? PrimaryCategory,
    DateTimeOffset UpdatedAt,
    CatalogImage? Image = null);

public sealed record CatalogImage(
    Guid Id,
    string Url,
    string AltText);

public sealed record CatalogDocument(
    Guid Id,
    string Url,
    string Title);

public interface IPublicMediaUrlResolver
{
    string? Resolve(string? storageKey);
}

public sealed record CatalogBrandSummary(
    Guid Id,
    string Name,
    string? Slug);

public sealed record CatalogBrandListItem(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    string? WebsiteUrl,
    CatalogImage? Logo,
    DateTimeOffset UpdatedAt);

public sealed record CatalogBrandDetail(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    string? WebsiteUrl,
    CatalogImage? Logo,
    IReadOnlyList<CatalogDocument> Catalogs,
    IReadOnlyList<CatalogCategorySummary> Categories,
    IReadOnlyList<CatalogProductSummary> Products,
    DateTimeOffset UpdatedAt);

public sealed record CatalogCategorySummary(
    Guid Id,
    string Name,
    string Slug);

public sealed record CatalogTagSummary(
    Guid Id,
    string Code,
    string Name,
    string Slug);

public sealed record CatalogNavigation(
    IReadOnlyList<CatalogSectionNavigationItem> Sections,
    IReadOnlyList<CatalogCategoryNavigationItem> Categories,
    IReadOnlyList<CatalogBrandSummary> Brands,
    IReadOnlyList<CatalogTagSummary> Tags,
    IReadOnlyList<CatalogTagSummary>? UsageAreas = null);

public sealed record CatalogSectionNavigationItem(
    Guid Id,
    string Code,
    string Name,
    string Slug);

public sealed record CatalogCategoryNavigationItem(
    Guid Id,
    Guid SectionId,
    Guid? ParentId,
    string Name,
    string Slug,
    string? MetaTitle = null,
    string? MetaDescription = null,
    string? CanonicalUrl = null,
    bool NoIndex = false,
    bool NoFollow = false,
    string? OpenGraphTitle = null,
    string? OpenGraphDescription = null,
    string? OpenGraphImageUrl = null);

public sealed record CatalogAttributeValue(
    Guid Id,
    Guid AttributeId,
    string Name,
    AttributeDataType DataType,
    int Sequence,
    string? TextValue,
    decimal? NumericValue,
    bool? BooleanValue,
    Guid? OptionId,
    string? OptionName,
    Guid? UnitId,
    string? UnitSymbol);

public sealed record CatalogProductDetail(
    Guid Id,
    string SKU,
    string Name,
    string Slug,
    string? ShortDescription,
    string? LongDescription,
    CatalogBrandSummary? Brand,
    IReadOnlyList<CatalogCategorySummary> Categories,
    IReadOnlyList<CatalogTagSummary> Tags,
    IReadOnlyList<CatalogAttributeValue> Attributes,
    IReadOnlyList<CatalogProductSummary> SimilarProducts,
    IReadOnlyList<CatalogProductSummary> ComplementaryProducts,
    IReadOnlyList<CatalogImage> Images,
    DateTimeOffset UpdatedAt,
    string? MetaTitle = null,
    string? MetaDescription = null,
    string? CanonicalUrl = null,
    bool NoIndex = false,
    bool NoFollow = false,
    string? OpenGraphTitle = null,
    string? OpenGraphDescription = null,
    string? OpenGraphImageUrl = null,
    IReadOnlyList<CatalogSeoAlternate>? Alternates = null);

public sealed record CatalogSeoAlternate(string LanguageCode, string Slug);
