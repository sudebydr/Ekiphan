using Ekiphan.Application.Content;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Ekiphan.Api.Content;

internal static class PressReleaseEndpoints
{
    public static void MapPressReleaseEndpoints(
        this IEndpointRouteBuilder endpoints,
        bool authenticationConfigured)
    {
        endpoints.MapGet("/api/press/{languageCode}", GetPublicAsync)
            .RequireRateLimiting("catalog-read");
        var admin = endpoints.MapGroup("/api/admin/content/press-releases")
            .RequireRateLimiting("catalog-admin-read");
        if (!authenticationConfigured)
        {
            admin.MapGet("", Unavailable);
            admin.MapPost("", Unavailable);
            admin.MapPut("/{id:guid}", (Guid id) => Unavailable());
            return;
        }

        admin.RequireAuthorization("ContentManage");
        admin.MapGet("", GetAdminAsync);
        admin.MapPost("", CreateAsync).Write();
        admin.MapPut("/{id:guid}", UpdateAsync).Write();
    }

    private static async Task<IResult> GetPublicAsync(
        string languageCode,
        IPressReleaseService service,
        CancellationToken cancellationToken)
    {
        var language = languageCode.ToLowerInvariant();
        if (language is not ("tr" or "en"))
        {
            return Problem(400, "languageCode must be tr or en.");
        }
        return Results.Ok(await service.GetPublicAsync(language, cancellationToken));
    }

    private static async Task<IResult> GetAdminAsync(
        HttpResponse response,
        IPressReleaseService service,
        CancellationToken cancellationToken)
    {
        NoStore(response);
        return Results.Ok(await service.GetAdminAsync(cancellationToken));
    }

    private static Task<IResult> CreateAsync(
        SavePressReleaseRequest request,
        HttpResponse response,
        IPressReleaseService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
        {
            var result = await service.CreateAsync(request.ToCommand(), cancellationToken);
            return Results.Created($"/api/admin/content/press-releases/{result.Id}", result);
        });

    private static Task<IResult> UpdateAsync(
        Guid id,
        SavePressReleaseRequest request,
        HttpResponse response,
        IPressReleaseService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
        {
            var result = await service.UpdateAsync(id, request.ToCommand(), cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

    private static RouteHandlerBuilder Write(this RouteHandlerBuilder builder) =>
        builder.WithMetadata(new RequestSizeLimitAttribute(64 * 1024))
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
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException)
        {
            return Problem(400, exception.Message);
        }
    }

    private static void NoStore(HttpResponse response)
    {
        response.Headers.CacheControl = "private, no-store";
        response.Headers.Pragma = "no-cache";
    }

    private static IResult Unavailable() =>
        Problem(503, "Press release administration is unavailable until JWT authentication is configured.");

    private static IResult Problem(int status, string detail) =>
        Results.Problem(
            statusCode: status,
            title: ReasonPhrases.GetReasonPhrase(status),
            detail: detail);

    private sealed record PressReleaseTranslationRequest(
        string LanguageCode,
        string Title,
        string Summary,
        string? Body,
        string? Slug,
        string? MetaTitle,
        string? MetaDescription,
        string? CanonicalUrl,
        bool NoIndex,
        bool NoFollow,
        string? OpenGraphTitle,
        string? OpenGraphDescription,
        Guid? OpenGraphImageMediaId);

    private sealed record SavePressReleaseRequest(
        Guid? CoverMediaId,
        Guid? AttachmentMediaId,
        DateTimeOffset PublishedAt,
        bool IsPublished,
        IReadOnlyList<PressReleaseTranslationRequest>? Translations)
    {
        public SavePressReleaseCommand ToCommand() => new(
            CoverMediaId,
            AttachmentMediaId,
            PublishedAt,
            IsPublished,
            Translations?.Select(item => new SavePressReleaseTranslation(
                item.LanguageCode, item.Title, item.Summary, item.Body,
                item.Slug, item.MetaTitle, item.MetaDescription,
                item.CanonicalUrl, item.NoIndex, item.NoFollow,
                item.OpenGraphTitle, item.OpenGraphDescription,
                item.OpenGraphImageMediaId)).ToArray() ?? []);
    }
}
