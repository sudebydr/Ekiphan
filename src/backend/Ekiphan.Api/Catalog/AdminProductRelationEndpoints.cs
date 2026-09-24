using Ekiphan.Application.Catalog;
using Ekiphan.Domain.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Ekiphan.Api.Catalog;

internal static class AdminProductRelationEndpoints
{
    public static void MapAdminProductRelationEndpoints(
        this IEndpointRouteBuilder endpoints,
        bool authenticationConfigured)
    {
        var group = endpoints.MapGroup("/api/admin/catalog")
            .RequireRateLimiting("catalog-admin-read");
        if (!authenticationConfigured)
        {
            group.MapGet("/products", AuthenticationUnavailable);
            group.MapGet(
                "/products/{productId:guid}/relations",
                AuthenticationUnavailableForProduct);
            group.MapPost(
                    "/products/{productId:guid}/relations",
                    AuthenticationUnavailableForProduct)
                .WithMetadata(new RequestSizeLimitAttribute(8 * 1024));
            group.MapDelete(
                "/relations/{relationId:guid}",
                AuthenticationUnavailableForRelation);
            return;
        }

        group.RequireAuthorization("CatalogManage");
        group.MapGet("/products", GetProductsAsync);
        group.MapGet(
            "/products/{productId:guid}/relations",
            GetRelationsAsync);
        group.MapPost(
                "/products/{productId:guid}/relations",
                CreateAsync)
            .WithMetadata(new RequestSizeLimitAttribute(8 * 1024))
            .RequireRateLimiting("catalog-admin-write");
        group.MapDelete(
                "/relations/{relationId:guid}",
                DeactivateAsync)
            .RequireRateLimiting("catalog-admin-write");
    }

    private static async Task<IResult> GetProductsAsync(
        HttpRequest request,
        HttpResponse response,
        IAdminProductRelationService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        if (!TryGetPagination(request, out var page, out var pageSize))
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "page must be at least 1 and pageSize must be between 1 and 100.");
        }

        if (!TryGetLanguage(request, out var language))
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "language must be tr or en.");
        }

        var search = request.Query.TryGetValue("search", out var rawSearch)
            ? rawSearch.ToString().Trim()
            : null;
        if (search is { Length: > 100 })
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "search cannot exceed 100 characters.");
        }

        if (!TryGetOptionalGuid(request, "categoryId", out var categoryId) ||
            !TryGetOptionalGuid(request, "brandId", out var brandId) ||
            !TryGetOptionalBoolean(request, "isPublished", out var isPublished) ||
            !TryGetBoolean(request, "missingImage", out var missingImage) ||
            !TryGetBoolean(request, "missingEnglish", out var missingEnglish) ||
            !TryGetSort(request, out var sortBy, out var sortDirection))
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "Product filters or sorting values are invalid.");
        }

        return Results.Ok(
            await service.GetProductsAsync(
                new AdminProductListQuery(
                    page,
                    pageSize,
                    language,
                    search,
                    categoryId,
                    brandId,
                    isPublished,
                    missingImage,
                    missingEnglish,
                    sortBy,
                    sortDirection),
                cancellationToken));
    }

    private static async Task<IResult> GetRelationsAsync(
        Guid productId,
        HttpRequest request,
        HttpResponse response,
        IAdminProductRelationService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        if (!TryGetLanguage(request, out var language))
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "language must be tr or en.");
        }

        try
        {
            return Results.Ok(
                await service.GetRelationsAsync(
                    productId,
                    language,
                    cancellationToken));
        }
        catch (CatalogProductNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> CreateAsync(
        Guid productId,
        CreateProductRelationRequest request,
        HttpRequest httpRequest,
        HttpResponse response,
        IAdminProductRelationService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        if (!TryGetLanguage(httpRequest, out var language) ||
            request.TargetProductId == Guid.Empty ||
            request.TargetProductId == productId ||
            !TryGetRelationType(request.RelationType, out var relationType))
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "A different target product and a valid relation type are required.");
        }

        try
        {
            var relation = await service.CreateAsync(
                new CreateAdminProductRelationCommand(
                    productId,
                    request.TargetProductId,
                    relationType,
                    request.IsBidirectional,
                    request.SortOrder),
                language,
                cancellationToken);
            return Results.Created(
                $"/api/admin/catalog/products/{productId}/relations",
                relation);
        }
        catch (CatalogProductNotFoundException)
        {
            return Results.NotFound();
        }
        catch (ProductRelationConflictException exception)
        {
            return Problem(StatusCodes.Status409Conflict, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return Problem(StatusCodes.Status400BadRequest, exception.Message);
        }
    }

    private static async Task<IResult> DeactivateAsync(
        Guid relationId,
        HttpResponse response,
        IAdminProductRelationService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        try
        {
            await service.DeactivateAsync(relationId, cancellationToken);
            return Results.NoContent();
        }
        catch (ProductRelationNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static bool TryGetRelationType(
        string value,
        out ProductRelationType relationType)
    {
        relationType = default;
        return !int.TryParse(value, out _) &&
            Enum.TryParse(value, true, out relationType) &&
            Enum.IsDefined(relationType);
    }

    private static bool TryGetLanguage(
        HttpRequest request,
        out string language)
    {
        language = request.Query.TryGetValue("language", out var rawLanguage)
            ? rawLanguage.ToString().ToLowerInvariant()
            : "tr";
        return language is "tr" or "en";
    }

    private static bool TryGetPagination(
        HttpRequest request,
        out int page,
        out int pageSize)
    {
        page = 1;
        pageSize = 20;
        return (!request.Query.TryGetValue("page", out var rawPage) ||
                int.TryParse(rawPage, out page)) &&
            (!request.Query.TryGetValue("pageSize", out var rawPageSize) ||
             int.TryParse(rawPageSize, out pageSize)) &&
            page >= 1 &&
            pageSize is >= 1 and <= 100;
    }

    private static bool TryGetOptionalGuid(
        HttpRequest request,
        string key,
        out Guid? value)
    {
        value = null;
        if (!request.Query.TryGetValue(key, out var raw) ||
            string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        if (!Guid.TryParse(raw, out var parsed) || parsed == Guid.Empty)
        {
            return false;
        }

        value = parsed;
        return true;
    }

    private static bool TryGetOptionalBoolean(
        HttpRequest request,
        string key,
        out bool? value)
    {
        value = null;
        if (!request.Query.TryGetValue(key, out var raw) ||
            string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        if (!bool.TryParse(raw, out var parsed))
        {
            return false;
        }

        value = parsed;
        return true;
    }

    private static bool TryGetBoolean(
        HttpRequest request,
        string key,
        out bool value)
    {
        value = false;
        return !request.Query.TryGetValue(key, out var raw) ||
            string.IsNullOrWhiteSpace(raw) ||
            bool.TryParse(raw, out value);
    }

    private static bool TryGetSort(
        HttpRequest request,
        out AdminProductSortBy sortBy,
        out AdminSortDirection direction)
    {
        var rawSort = request.Query.TryGetValue("sortBy", out var sort)
            ? sort.ToString()
            : "updatedAt";
        var rawDirection = request.Query.TryGetValue("sortDirection", out var order)
            ? order.ToString()
            : "desc";
        sortBy = rawSort.ToLowerInvariant() switch
        {
            "name" => AdminProductSortBy.Name,
            "sku" => AdminProductSortBy.SKU,
            "updatedat" => AdminProductSortBy.UpdatedAt,
            "createdat" => AdminProductSortBy.CreatedAt,
            _ => default,
        };
        direction = rawDirection.ToLowerInvariant() switch
        {
            "asc" => AdminSortDirection.Ascending,
            "desc" => AdminSortDirection.Descending,
            _ => default,
        };
        return Enum.IsDefined(sortBy) && Enum.IsDefined(direction);
    }

    private static void SetPrivateNoStore(HttpResponse response)
    {
        response.Headers.CacheControl = "private, no-store";
        response.Headers.Pragma = "no-cache";
    }

    private static IResult AuthenticationUnavailable() =>
        Problem(
            StatusCodes.Status503ServiceUnavailable,
            "Catalog administration is unavailable until JWT authentication is configured.");

    private static IResult AuthenticationUnavailableForProduct(Guid productId) =>
        AuthenticationUnavailable();

    private static IResult AuthenticationUnavailableForRelation(Guid relationId) =>
        AuthenticationUnavailable();

    private static IResult Problem(int statusCode, string detail) =>
        Results.Problem(
            statusCode: statusCode,
            title: ReasonPhrases.GetReasonPhrase(statusCode),
            detail: detail);

    private sealed record CreateProductRelationRequest(
        Guid TargetProductId,
        string RelationType,
        bool IsBidirectional,
        int SortOrder);
}
