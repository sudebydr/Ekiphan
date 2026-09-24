using Ekiphan.Application.Content;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Ekiphan.Api.Content;

internal static class HomepageHeroEndpoints
{
    public static void MapHomepageHeroEndpoints(
        this IEndpointRouteBuilder endpoints,
        bool authenticationConfigured)
    {
        endpoints.MapGet("/api/home/{languageCode}/heroes", GetPublicAsync)
            .RequireRateLimiting("catalog-read");
        var admin = endpoints.MapGroup("/api/admin/content/homepage-heroes")
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
        IHomepageHeroService service,
        CancellationToken cancellationToken)
    {
        var language = languageCode.ToLowerInvariant();
        if (language is not ("tr" or "en"))
            return Problem(400, "languageCode must be tr or en.");
        return Results.Ok(await service.GetPublicAsync(language, cancellationToken));
    }

    private static async Task<IResult> GetAdminAsync(
        HttpResponse response,
        IHomepageHeroService service,
        CancellationToken cancellationToken)
    {
        NoStore(response);
        return Results.Ok(await service.GetAdminAsync(cancellationToken));
    }

    private static Task<IResult> CreateAsync(
        SaveHeroRequest request,
        HttpResponse response,
        IHomepageHeroService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
        {
            var result = await service.CreateAsync(request.ToCommand(), cancellationToken);
            return Results.Created($"/api/admin/content/homepage-heroes/{result.Id}", result);
        });

    private static Task<IResult> UpdateAsync(
        Guid id,
        SaveHeroRequest request,
        HttpResponse response,
        IHomepageHeroService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
        {
            var result = await service.UpdateAsync(id, request.ToCommand(), cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

    private static RouteHandlerBuilder Write(this RouteHandlerBuilder builder) =>
        builder.WithMetadata(new RequestSizeLimitAttribute(32 * 1024))
            .RequireRateLimiting("catalog-admin-write");

    private static async Task<IResult> Execute(
        HttpResponse response,
        Func<Task<IResult>> operation)
    {
        NoStore(response);
        try { return await operation(); }
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
        Problem(503, "Homepage administration is unavailable until JWT authentication is configured.");

    private static IResult Problem(int status, string detail) =>
        Results.Problem(statusCode: status,
            title: ReasonPhrases.GetReasonPhrase(status), detail: detail);

    private sealed record HeroTranslationRequest(
        string LanguageCode,
        string Title,
        string? Subtitle,
        string PrimaryCtaLabel,
        string PrimaryCtaUrl,
        string? SecondaryCtaLabel,
        string? SecondaryCtaUrl);

    private sealed record SaveHeroRequest(
        Guid? DesktopMediaId,
        Guid? MobileMediaId,
        int SortOrder,
        bool IsPublished,
        DateTimeOffset? StartsAt,
        DateTimeOffset? EndsAt,
        IReadOnlyList<HeroTranslationRequest>? Translations)
    {
        public SaveHomepageHeroCommand ToCommand() => new(
            DesktopMediaId, MobileMediaId, SortOrder, IsPublished,
            StartsAt, EndsAt,
            Translations?.Select(item => new SaveHomepageHeroTranslation(
                item.LanguageCode, item.Title, item.Subtitle,
                item.PrimaryCtaLabel, item.PrimaryCtaUrl,
                item.SecondaryCtaLabel, item.SecondaryCtaUrl)).ToArray() ?? []);
    }
}
