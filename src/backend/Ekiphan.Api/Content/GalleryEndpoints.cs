using Ekiphan.Application.Content;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Ekiphan.Api.Content;

internal static class GalleryEndpoints
{
    public static void MapGalleryEndpoints(
        this IEndpointRouteBuilder endpoints,
        bool authenticationConfigured)
    {
        endpoints.MapGet("/api/gallery/{languageCode}", GetPublicAsync)
            .RequireRateLimiting("catalog-read");
        var admin = endpoints.MapGroup("/api/admin/content/gallery")
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
        IGalleryService service,
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
        IGalleryService service,
        CancellationToken cancellationToken)
    {
        NoStore(response);
        return Results.Ok(await service.GetAdminAsync(cancellationToken));
    }

    private static Task<IResult> CreateAsync(
        SaveGalleryRequest request,
        HttpResponse response,
        IGalleryService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
        {
            var result = await service.CreateAsync(request.ToCommand(), cancellationToken);
            return Results.Created($"/api/admin/content/gallery/{result.Id}", result);
        });

    private static Task<IResult> UpdateAsync(
        Guid id,
        SaveGalleryRequest request,
        HttpResponse response,
        IGalleryService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
        {
            var result = await service.UpdateAsync(id, request.ToCommand(), cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

    private static RouteHandlerBuilder Write(this RouteHandlerBuilder builder) =>
        builder.WithMetadata(new RequestSizeLimitAttribute(24 * 1024))
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
        Problem(503, "Gallery administration is unavailable until JWT authentication is configured.");

    private static IResult Problem(int status, string detail) =>
        Results.Problem(
            statusCode: status,
            title: ReasonPhrases.GetReasonPhrase(status),
            detail: detail);

    private sealed record GalleryTranslationRequest(
        string LanguageCode,
        string Title,
        string? Caption);

    private sealed record SaveGalleryRequest(
        Guid MediaAssetId,
        int SortOrder,
        bool IsPublished,
        IReadOnlyList<GalleryTranslationRequest>? Translations)
    {
        public SaveGalleryItemCommand ToCommand() => new(
            MediaAssetId,
            SortOrder,
            IsPublished,
            Translations?.Select(item => new SaveGalleryTranslation(
                item.LanguageCode,
                item.Title,
                item.Caption)).ToArray() ?? []);
    }
}
