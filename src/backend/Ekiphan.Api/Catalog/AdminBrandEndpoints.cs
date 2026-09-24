using Ekiphan.Application.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Ekiphan.Api.Catalog;

internal static class AdminBrandEndpoints
{
    private const int MaximumRequestBytes = 16 * 1024;

    public static void MapAdminBrandEndpoints(
        this IEndpointRouteBuilder endpoints,
        bool authenticationConfigured)
    {
        var group = endpoints.MapGroup("/api/admin/catalog/brands")
            .RequireRateLimiting("catalog-admin-read");
        if (!authenticationConfigured)
        {
            group.MapGet("", AuthenticationUnavailable);
            group.MapGet("/{brandId:guid}", AuthenticationUnavailableForId);
            group.MapPost("", AuthenticationUnavailable)
                .WithMetadata(new RequestSizeLimitAttribute(MaximumRequestBytes));
            group.MapPut("/{brandId:guid}", AuthenticationUnavailableForId)
                .WithMetadata(new RequestSizeLimitAttribute(MaximumRequestBytes));
            group.MapDelete("/{brandId:guid}", AuthenticationUnavailableForId);
            return;
        }

        group.RequireAuthorization("CatalogManage");
        group.MapGet("", GetPageAsync);
        group.MapGet("/{brandId:guid}", GetAsync);
        group.MapPost("", CreateAsync)
            .WithMetadata(new RequestSizeLimitAttribute(MaximumRequestBytes))
            .RequireRateLimiting("catalog-admin-write");
        group.MapPut("/{brandId:guid}", UpdateAsync)
            .WithMetadata(new RequestSizeLimitAttribute(MaximumRequestBytes))
            .RequireRateLimiting("catalog-admin-write");
        group.MapDelete("/{brandId:guid}", ArchiveAsync)
            .RequireRateLimiting("catalog-admin-write");
    }

    private static async Task<IResult> GetPageAsync(
        HttpRequest request,
        HttpResponse response,
        IAdminBrandService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        if (!TryPage(request, out var page, out var pageSize) ||
            !TryLanguage(request, out var language))
        {
            return Problem(StatusCodes.Status400BadRequest, "Invalid paging or language.");
        }

        var search = request.Query.TryGetValue("search", out var raw)
            ? raw.ToString().Trim()
            : null;
        if (search is { Length: > 100 })
        {
            return Problem(StatusCodes.Status400BadRequest, "search cannot exceed 100 characters.");
        }

        return Results.Ok(
            await service.GetPageAsync(
                page,
                pageSize,
                language,
                search,
                cancellationToken));
    }

    private static async Task<IResult> GetAsync(
        Guid brandId,
        HttpResponse response,
        IAdminBrandService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        var brand = await service.GetAsync(brandId, cancellationToken);
        return brand is null ? Results.NotFound() : Results.Ok(brand);
    }

    private static async Task<IResult> CreateAsync(
        SaveBrandRequest request,
        HttpResponse response,
        IAdminBrandService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        try
        {
            var brand = await service.CreateAsync(
                request.ToCommand(),
                cancellationToken);
            return Results.Created(
                $"/api/admin/catalog/brands/{brand.Id}",
                brand);
        }
        catch (AdminBrandConflictException exception)
        {
            return Problem(StatusCodes.Status409Conflict, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return Problem(StatusCodes.Status400BadRequest, exception.Message);
        }
    }

    private static async Task<IResult> UpdateAsync(
        Guid brandId,
        SaveBrandRequest request,
        HttpResponse response,
        IAdminBrandService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        try
        {
            var brand = await service.UpdateAsync(
                brandId,
                request.ToCommand(),
                cancellationToken);
            return brand is null ? Results.NotFound() : Results.Ok(brand);
        }
        catch (AdminBrandConflictException exception)
        {
            return Problem(StatusCodes.Status409Conflict, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return Problem(StatusCodes.Status400BadRequest, exception.Message);
        }
    }

    private static async Task<IResult> ArchiveAsync(
        Guid brandId,
        HttpResponse response,
        IAdminBrandService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        try
        {
            return await service.ArchiveAsync(brandId, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound();
        }
        catch (AdminBrandConflictException exception)
        {
            return Problem(StatusCodes.Status409Conflict, exception.Message);
        }
    }

    private static bool TryPage(
        HttpRequest request,
        out int page,
        out int pageSize)
    {
        page = 1;
        pageSize = 50;
        return (!request.Query.TryGetValue("page", out var rawPage) ||
                int.TryParse(rawPage, out page)) &&
            (!request.Query.TryGetValue("pageSize", out var rawPageSize) ||
             int.TryParse(rawPageSize, out pageSize)) &&
            page >= 1 &&
            pageSize is >= 1 and <= 100;
    }

    private static bool TryLanguage(
        HttpRequest request,
        out string language)
    {
        language = request.Query.TryGetValue("language", out var raw)
            ? raw.ToString().ToLowerInvariant()
            : "tr";
        return language is "tr" or "en";
    }

    private static void SetPrivateNoStore(HttpResponse response)
    {
        response.Headers.CacheControl = "private, no-store";
        response.Headers.Pragma = "no-cache";
    }

    private static IResult AuthenticationUnavailable() =>
        Problem(
            StatusCodes.Status503ServiceUnavailable,
            "Brand administration is unavailable until JWT authentication is configured.");

    private static IResult AuthenticationUnavailableForId(Guid brandId) =>
        AuthenticationUnavailable();

    private static IResult Problem(int statusCode, string detail) =>
        Results.Problem(
            statusCode: statusCode,
            title: ReasonPhrases.GetReasonPhrase(statusCode),
            detail: detail);

    private sealed record SaveBrandRequest(
        string Name,
        string? WebsiteUrl,
        int SortOrder,
        bool IsPublished,
        IReadOnlyList<BrandTranslationRequest>? Translations)
    {
        public SaveAdminBrandCommand ToCommand() =>
            new(
                Name,
                WebsiteUrl,
                SortOrder,
                IsPublished,
                Translations?
                    .Select(item => new AdminBrandTranslationInput(
                        item.LanguageCode,
                        item.Description,
                        item.Slug))
                    .ToArray() ?? []);
    }

    private sealed record BrandTranslationRequest(
        string LanguageCode,
        string Description,
        string Slug);
}
