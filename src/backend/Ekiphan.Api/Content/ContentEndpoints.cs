using Ekiphan.Application.Content;
using Ekiphan.Domain.Content;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Ekiphan.Api.Content;

internal static class ContentEndpoints
{
    public static void MapContentEndpoints(
        this IEndpointRouteBuilder endpoints,
        bool authenticationConfigured)
    {
        endpoints.MapGet(
                "/api/content/{languageCode}/pages/{slug}",
                GetPublishedAsync)
            .RequireRateLimiting("catalog-read");
        endpoints.MapGet(
                "/api/content/{languageCode}/sitemap",
                GetSitemapAsync)
            .RequireRateLimiting("catalog-read");

        var admin = endpoints.MapGroup("/api/admin/content/pages")
            .RequireRateLimiting("catalog-admin-read");
        if (!authenticationConfigured)
        {
            admin.MapGet("", Unavailable);
            admin.MapPost("", Unavailable);
            admin.MapPut("/{id:guid}", UnavailableForId);
            return;
        }

        admin.RequireAuthorization("ContentManage");
        admin.MapGet("", GetAdminAsync);
        admin.MapPost("", CreateAsync).Write();
        admin.MapPut("/{id:guid}", UpdateAsync).Write();
    }

    private static async Task<IResult> GetPublishedAsync(
        string languageCode,
        string slug,
        HttpResponse response,
        IContentPageService service,
        CancellationToken cancellationToken)
    {
        if (!languageCode.Equals("tr", StringComparison.OrdinalIgnoreCase) &&
            !languageCode.Equals("en", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(slug) ||
            slug.Length > 200)
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "Language must be tr or en and slug must be valid.");
        }

        response.Headers.CacheControl = "public, max-age=60, stale-while-revalidate=300";
        var value = await service.GetPublishedAsync(
            languageCode.ToLowerInvariant(),
            slug,
            cancellationToken);
        return value is null ? Results.NotFound() : Results.Ok(value);
    }

    private static async Task<IResult> GetSitemapAsync(
        string languageCode,
        IContentPageService service,
        CancellationToken cancellationToken)
    {
        if (!languageCode.Equals("tr", StringComparison.OrdinalIgnoreCase) &&
            !languageCode.Equals("en", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "Language must be tr or en.");
        }

        return Results.Ok(
            await service.GetSitemapAsync(
                languageCode.ToLowerInvariant(),
                cancellationToken));
    }

    private static async Task<IResult> GetAdminAsync(
        HttpResponse response,
        IContentPageService service,
        CancellationToken cancellationToken)
    {
        NoStore(response);
        return Results.Ok(
            await service.GetAdminPagesAsync(cancellationToken));
    }

    private static Task<IResult> CreateAsync(
        SavePageRequest request,
        HttpResponse response,
        IContentPageService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
        {
            var value = await service.CreateAsync(
                request.ToCommand(),
                cancellationToken);
            return Results.Created(
                $"/api/admin/content/pages/{value.Id}",
                value);
        });

    private static Task<IResult> UpdateAsync(
        Guid id,
        SavePageRequest request,
        HttpResponse response,
        IContentPageService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
        {
            var value = await service.UpdateAsync(
                id,
                request.ToCommand(),
                cancellationToken);
            return value is null ? Results.NotFound() : Results.Ok(value);
        });

    private static RouteHandlerBuilder Write(this RouteHandlerBuilder builder) =>
        builder
            .WithMetadata(new RequestSizeLimitAttribute(128 * 1024))
            .RequireRateLimiting("catalog-admin-write");

    private static async Task<IResult> Execute(
        HttpResponse response,
        Func<Task<IResult>> operation)
    {
        NoStore(response);
        try
        {
            return await operation();
        }
        catch (ContentPageConflictException exception)
        {
            return Problem(StatusCodes.Status409Conflict, exception.Message);
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException)
        {
            return Problem(StatusCodes.Status400BadRequest, exception.Message);
        }
    }

    private static void NoStore(HttpResponse response)
    {
        response.Headers.CacheControl = "private, no-store";
        response.Headers.Pragma = "no-cache";
    }

    private static IResult Unavailable() =>
        Problem(
            StatusCodes.Status503ServiceUnavailable,
            "Content administration is unavailable until JWT authentication is configured.");

    private static IResult UnavailableForId(Guid id) => Unavailable();

    private static IResult Problem(int statusCode, string detail) =>
        Results.Problem(
            statusCode: statusCode,
            title: ReasonPhrases.GetReasonPhrase(statusCode),
            detail: detail);

    private sealed record SaveTranslationRequest(
        string LanguageCode,
        string Title,
        string Slug,
        string? Summary,
        string Body,
        string? MetaTitle,
        string? MetaDescription,
        string? CanonicalUrl,
        bool NoIndex,
        bool NoFollow,
        string? OpenGraphTitle,
        string? OpenGraphDescription,
        Guid? OpenGraphImageMediaId);

    private sealed record SavePageRequest(
        string Code,
        string Status,
        IReadOnlyList<SaveTranslationRequest>? Translations)
    {
        public SaveContentPageCommand ToCommand()
        {
            if (int.TryParse(Status, out _) ||
                !Enum.TryParse<ContentStatus>(
                    Status,
                    ignoreCase: true,
                    out var status) ||
                !Enum.IsDefined(status))
            {
                throw new ArgumentException(
                    "Status must be Draft, Review, Published or Archived.");
            }

            return new SaveContentPageCommand(
                Code,
                status,
                Translations?.Select(item => new SaveContentTranslation(
                    item.LanguageCode,
                    item.Title,
                    item.Slug,
                    item.Summary,
                    item.Body,
                    item.MetaTitle,
                    item.MetaDescription,
                    item.CanonicalUrl,
                    item.NoIndex,
                    item.NoFollow,
                    item.OpenGraphTitle,
                    item.OpenGraphDescription,
                    item.OpenGraphImageMediaId)).ToArray() ?? []);
        }
    }
}
