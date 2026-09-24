using Ekiphan.Application.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Ekiphan.Api.Catalog;

internal static class AdminProductManagementEndpoints
{
    private const int MaximumRequestBytes = 32 * 1024;

    public static void MapAdminProductManagementEndpoints(
        this IEndpointRouteBuilder endpoints,
        bool authenticationConfigured)
    {
        var group = endpoints.MapGroup("/api/admin/catalog/products")
            .RequireRateLimiting("catalog-admin-read");
        if (!authenticationConfigured)
        {
            group.MapGet("/{productId:guid}", AuthenticationUnavailableForId);
            group.MapPost("", AuthenticationUnavailable)
                .WithMetadata(
                    new RequestSizeLimitAttribute(MaximumRequestBytes));
            group.MapPut("/{productId:guid}", AuthenticationUnavailableForId)
                .WithMetadata(
                    new RequestSizeLimitAttribute(MaximumRequestBytes));
            group.MapDelete(
                "/{productId:guid}",
                AuthenticationUnavailableForId);
            return;
        }

        group.RequireAuthorization("CatalogManage");
        group.MapGet("/{productId:guid}", GetAsync);
        group.MapPost("", CreateAsync)
            .WithMetadata(
                new RequestSizeLimitAttribute(MaximumRequestBytes))
            .RequireRateLimiting("catalog-admin-write");
        group.MapPut("/{productId:guid}", UpdateAsync)
            .WithMetadata(
                new RequestSizeLimitAttribute(MaximumRequestBytes))
            .RequireRateLimiting("catalog-admin-write");
        group.MapDelete("/{productId:guid}", SoftDeleteAsync)
            .RequireRateLimiting("catalog-admin-write");
    }

    private static async Task<IResult> GetAsync(
        Guid productId,
        HttpResponse response,
        IAdminProductManagementService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        var product = await service.GetAsync(productId, cancellationToken);
        return product is null ? Results.NotFound() : Results.Ok(product);
    }

    private static async Task<IResult> CreateAsync(
        SaveProductRequest request,
        HttpResponse response,
        IAdminProductManagementService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        try
        {
            var product = await service.CreateAsync(
                request.ToCommand(),
                cancellationToken);
            return Results.Created(
                $"/api/admin/catalog/products/{product.Id}",
                product);
        }
        catch (AdminProductConflictException exception)
        {
            return Problem(StatusCodes.Status409Conflict, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return Problem(StatusCodes.Status400BadRequest, exception.Message);
        }
    }

    private static async Task<IResult> UpdateAsync(
        Guid productId,
        SaveProductRequest request,
        HttpResponse response,
        IAdminProductManagementService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        try
        {
            var product = await service.UpdateAsync(
                productId,
                request.ToCommand(),
                cancellationToken);
            return product is null
                ? Results.NotFound()
                : Results.Ok(product);
        }
        catch (AdminProductConflictException exception)
        {
            return Problem(StatusCodes.Status409Conflict, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return Problem(StatusCodes.Status400BadRequest, exception.Message);
        }
    }

    private static async Task<IResult> SoftDeleteAsync(
        Guid productId,
        HttpResponse response,
        IAdminProductManagementService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        return await service.SoftDeleteAsync(productId, cancellationToken)
            ? Results.NoContent()
            : Results.NotFound();
    }

    private static void SetPrivateNoStore(HttpResponse response)
    {
        response.Headers.CacheControl = "private, no-store";
        response.Headers.Pragma = "no-cache";
    }

    private static IResult AuthenticationUnavailable() =>
        Problem(
            StatusCodes.Status503ServiceUnavailable,
            "Product administration is unavailable until JWT authentication is configured.");

    private static IResult AuthenticationUnavailableForId(Guid productId) =>
        AuthenticationUnavailable();

    private static IResult Problem(int statusCode, string detail) =>
        Results.Problem(
            statusCode: statusCode,
            title: ReasonPhrases.GetReasonPhrase(statusCode),
            detail: detail);

    private sealed record SaveProductRequest(
        string SKU,
        Guid? BrandId,
        bool IsPublished,
        IReadOnlyList<ProductTranslationRequest>? Translations,
        IReadOnlyList<Guid>? CategoryIds,
        Guid? PrimaryCategoryId,
        IReadOnlyList<ProductAttributeValueRequest>? AttributeValues,
        IReadOnlyList<Guid>? TagIds)
    {
        public SaveAdminProductCommand ToCommand() =>
            new(
                SKU,
                BrandId,
                IsPublished,
                Translations?
                    .Select(translation =>
                        new AdminProductTranslationInput(
                            translation.LanguageCode,
                            translation.Name,
                            translation.Slug,
                            translation.ShortDescription,
                            translation.LongDescription,
                            translation.MetaTitle,
                            translation.MetaDescription,
                            translation.CanonicalUrl,
                            translation.NoIndex,
                            translation.NoFollow,
                            translation.OpenGraphTitle,
                            translation.OpenGraphDescription,
                            translation.OpenGraphImageMediaId))
                    .ToArray() ?? [],
                CategoryIds ?? [],
                PrimaryCategoryId,
                AttributeValues?.Select(item =>
                    new AdminProductAttributeValueInput(
                        item.AttributeId,
                        item.Sequence,
                        item.TextValue,
                        item.NumericValue,
                        item.BooleanValue,
                        item.AttributeOptionId,
                        item.UnitId)).ToArray() ?? [],
                TagIds ?? []);
    }

    private sealed record ProductTranslationRequest(
        string LanguageCode,
        string Name,
        string Slug,
        string? ShortDescription,
        string? LongDescription,
        string? MetaTitle,
        string? MetaDescription,
        string? CanonicalUrl,
        bool NoIndex,
        bool NoFollow,
        string? OpenGraphTitle,
        string? OpenGraphDescription,
        Guid? OpenGraphImageMediaId);

    private sealed record ProductAttributeValueRequest(
        Guid AttributeId,
        int Sequence,
        string? TextValue,
        decimal? NumericValue,
        bool? BooleanValue,
        Guid? AttributeOptionId,
        Guid? UnitId);
}
