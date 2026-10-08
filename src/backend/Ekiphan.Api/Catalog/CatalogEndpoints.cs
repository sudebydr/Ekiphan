using Ekiphan.Application.Catalog;
using Microsoft.AspNetCore.WebUtilities;

namespace Ekiphan.Api.Catalog;

internal static class CatalogEndpoints
{
    public static RouteGroupBuilder MapCatalogEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/catalog/{languageCode}")
            .RequireRateLimiting("catalog-read");
        group.MapGet("/navigation", GetNavigationAsync);
        group.MapGet("/facets", GetFacetsAsync);
        group.MapGet("/sitemap", GetSitemapAsync);
        group.MapGet("/brands", GetBrandsAsync);
        group.MapGet("/brands/{slug}", GetBrandAsync);
        group.MapGet("/products", ListProductsAsync);
        group.MapGet("/products/{slug}", GetProductAsync);
        return group;
    }

    private static async Task<IResult> GetNavigationAsync(
        string languageCode,
        ICatalogQueryService queryService,
        CancellationToken cancellationToken)
    {
        if (!IsLanguageSupported(languageCode))
        {
            return InvalidLanguage();
        }

        return Results.Ok(
            await queryService.GetNavigationAsync(
                languageCode.ToLowerInvariant(),
                cancellationToken));
    }

    private static async Task<IResult> GetSitemapAsync(
        string languageCode,
        ICatalogQueryService queryService,
        CancellationToken cancellationToken)
    {
        if (!IsLanguageSupported(languageCode))
        {
            return InvalidLanguage();
        }

        return Results.Ok(
            await queryService.GetSitemapEntriesAsync(
                languageCode.ToLowerInvariant(),
                cancellationToken));
    }

    private static async Task<IResult> GetFacetsAsync(
        string languageCode,
        HttpRequest request,
        ICatalogQueryService queryService,
        CancellationToken cancellationToken)
    {
        if (!IsLanguageSupported(languageCode))
        {
            return InvalidLanguage();
        }
        if (!TryGetOptionalText(request, "category", 250, out var category) ||
            category is null)
        {
            return Problem(400, "category must contain between 1 and 250 characters.");
        }
        return Results.Ok(await queryService.GetFacetsAsync(
            languageCode.ToLowerInvariant(), category!, cancellationToken));
    }

    private static async Task<IResult> ListProductsAsync(
        string languageCode,
        HttpRequest request,
        ICatalogQueryService queryService,
        CancellationToken cancellationToken)
    {
        if (!IsLanguageSupported(languageCode))
        {
            return InvalidLanguage();
        }

        if (!TryGetInt(request, "page", 1, out var page) ||
            !TryGetInt(request, "pageSize", 24, out var pageSize) ||
            page < 1 ||
            pageSize is < 1 or > 100)
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "page must be at least 1 and pageSize must be between 1 and 100.");
        }

        if (!TryGetSort(request, out var sort))
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "sort must be Name, NameDescending or Newest.");
        }

        if (!TryGetOptionalText(request, "q", 100, out var search) ||
            !TryGetOptionalText(request, "section", 200, out var section) ||
            !TryGetOptionalText(request, "category", 250, out var category) ||
            !TryGetOptionalText(request, "brand", 200, out var brand) ||
            !TryGetOptionalText(request, "tag", 200, out var tag) ||
            !TryGetOptionalText(request, "usage", 2000, out var usage))
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "A filter is empty or exceeds its maximum length.");
        }

        if (!TryGetAttributeFilters(request, out var attributeFilters))
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "attribute filters must use attributeId:value and contain at most 20 values.");
        }

        if (search is { Length: < 2 })
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "q must contain at least 2 characters.");
        }

        var result = await queryService.GetProductsAsync(
            new CatalogProductQuery(
                languageCode.ToLowerInvariant(),
                page,
                pageSize,
                search,
                section,
                category,
                brand,
                tag,
                sort,
                attributeFilters,
                usage),
            cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetBrandsAsync(
        string languageCode,
        ICatalogQueryService queryService,
        CancellationToken cancellationToken)
    {
        if (!IsLanguageSupported(languageCode))
        {
            return InvalidLanguage();
        }

        return Results.Ok(
            await queryService.GetBrandsAsync(
                languageCode.ToLowerInvariant(),
                cancellationToken));
    }

    private static async Task<IResult> GetBrandAsync(
        string languageCode,
        string slug,
        ICatalogQueryService queryService,
        CancellationToken cancellationToken)
    {
        if (!IsLanguageSupported(languageCode))
        {
            return InvalidLanguage();
        }

        if (string.IsNullOrWhiteSpace(slug) || slug.Length > 200)
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "slug must contain between 1 and 200 characters.");
        }

        var brand = await queryService.GetBrandAsync(
            languageCode.ToLowerInvariant(),
            slug,
            cancellationToken);
        return brand is null ? Results.NotFound() : Results.Ok(brand);
    }

    private static async Task<IResult> GetProductAsync(
        string languageCode,
        string slug,
        ICatalogQueryService queryService,
        CancellationToken cancellationToken)
    {
        if (!IsLanguageSupported(languageCode))
        {
            return InvalidLanguage();
        }

        if (string.IsNullOrWhiteSpace(slug) || slug.Length > 300)
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "slug must contain between 1 and 300 characters.");
        }

        var product = await queryService.GetProductAsync(
            languageCode.ToLowerInvariant(),
            slug,
            cancellationToken);
        return product is null ? Results.NotFound() : Results.Ok(product);
    }

    private static bool IsLanguageSupported(string languageCode) =>
        languageCode.Equals("tr", StringComparison.OrdinalIgnoreCase) ||
        languageCode.Equals("en", StringComparison.OrdinalIgnoreCase);

    private static IResult InvalidLanguage() =>
        Problem(
            StatusCodes.Status400BadRequest,
            "languageCode must be tr or en.");

    private static bool TryGetInt(
        HttpRequest request,
        string key,
        int defaultValue,
        out int value)
    {
        value = defaultValue;
        return !request.Query.TryGetValue(key, out var rawValue) ||
            int.TryParse(rawValue, out value);
    }

    private static bool TryGetSort(
        HttpRequest request,
        out CatalogProductSort sort)
    {
        sort = CatalogProductSort.Name;
        if (!request.Query.TryGetValue("sort", out var rawValue))
        {
            return true;
        }

        return !int.TryParse(rawValue, out _) &&
            Enum.TryParse(rawValue, ignoreCase: true, out sort) &&
            Enum.IsDefined(sort);
    }

    private static bool TryGetOptionalText(
        HttpRequest request,
        string key,
        int maximumLength,
        out string? value)
    {
        value = null;
        if (!request.Query.TryGetValue(key, out var rawValue))
        {
            return true;
        }

        value = rawValue.ToString().Trim();
        return value.Length is > 0 && value.Length <= maximumLength;
    }

    private static bool TryGetAttributeFilters(
        HttpRequest request,
        out IReadOnlyList<CatalogAttributeFilter> filters)
    {
        filters = [];
        if (!request.Query.TryGetValue("attribute", out var rawValues))
        {
            return true;
        }
        if (rawValues.Count is < 1 or > 20)
        {
            return false;
        }
        var parsed = new List<CatalogAttributeFilter>(rawValues.Count);
        foreach (var raw in rawValues)
        {
            var text = raw?.Trim() ?? string.Empty;
            var separator = text.IndexOf(':');
            if (separator < 1 ||
                !Guid.TryParse(text[..separator], out var attributeId) ||
                attributeId == Guid.Empty ||
                text[(separator + 1)..] is not { Length: > 0 and <= 300 } value)
            {
                return false;
            }
            parsed.Add(new CatalogAttributeFilter(attributeId, value));
        }
        filters = parsed;
        return true;
    }

    private static IResult Problem(int statusCode, string detail) =>
        Results.Problem(
            statusCode: statusCode,
            title: ReasonPhrases.GetReasonPhrase(statusCode),
            detail: detail);
}
