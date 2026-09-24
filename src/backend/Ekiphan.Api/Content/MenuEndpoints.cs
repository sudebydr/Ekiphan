using Ekiphan.Application.Content;
using Ekiphan.Domain.Content;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Ekiphan.Api.Content;

internal static class MenuEndpoints
{
    public static void MapMenuEndpoints(
        this IEndpointRouteBuilder endpoints,
        bool authenticationConfigured)
    {
        endpoints.MapGet(
                "/api/navigation/{languageCode}/{location}",
                GetPublicAsync)
            .RequireRateLimiting("catalog-read");

        var admin = endpoints.MapGroup("/api/admin/content/menu-items")
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

    private static async Task<IResult> GetPublicAsync(
        string languageCode,
        string location,
        HttpResponse response,
        IMenuService service,
        CancellationToken cancellationToken)
    {
        if (!TryLanguage(languageCode, out var language) ||
            !TryLocation(location, out var menuLocation))
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "Language must be tr or en and location must be Header or Footer.");
        }

        response.Headers.CacheControl =
            "public, max-age=60, stale-while-revalidate=300";
        return Results.Ok(
            await service.GetPublicItemsAsync(
                language,
                menuLocation,
                cancellationToken));
    }

    private static async Task<IResult> GetAdminAsync(
        HttpResponse response,
        IMenuService service,
        CancellationToken cancellationToken)
    {
        NoStore(response);
        return Results.Ok(
            await service.GetAdminItemsAsync(cancellationToken));
    }

    private static Task<IResult> CreateAsync(
        SaveMenuRequest request,
        HttpResponse response,
        IMenuService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
        {
            var value = await service.CreateAsync(
                request.ToCommand(),
                cancellationToken);
            return Results.Created(
                $"/api/admin/content/menu-items/{value.Id}",
                value);
        });

    private static Task<IResult> UpdateAsync(
        Guid id,
        SaveMenuRequest request,
        HttpResponse response,
        IMenuService service,
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
            .WithMetadata(new RequestSizeLimitAttribute(24 * 1024))
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
        catch (MenuConflictException exception)
        {
            return Problem(StatusCodes.Status409Conflict, exception.Message);
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException)
        {
            return Problem(StatusCodes.Status400BadRequest, exception.Message);
        }
    }

    private static bool TryLanguage(string value, out string language)
    {
        language = value.Trim().ToLowerInvariant();
        return language is "tr" or "en";
    }

    private static bool TryLocation(
        string value,
        out MenuLocation location)
    {
        location = default;
        return !int.TryParse(value, out _) &&
            Enum.TryParse(value, ignoreCase: true, out location) &&
            Enum.IsDefined(location);
    }

    private static void NoStore(HttpResponse response)
    {
        response.Headers.CacheControl = "private, no-store";
        response.Headers.Pragma = "no-cache";
    }

    private static IResult Unavailable() =>
        Problem(
            StatusCodes.Status503ServiceUnavailable,
            "Menu administration is unavailable until JWT authentication is configured.");

    private static IResult UnavailableForId(Guid id) => Unavailable();

    private static IResult Problem(int statusCode, string detail) =>
        Results.Problem(
            statusCode: statusCode,
            title: ReasonPhrases.GetReasonPhrase(statusCode),
            detail: detail);

    private sealed record SaveMenuTranslationRequest(
        string LanguageCode,
        string Label);

    private sealed record SaveMenuRequest(
        string Code,
        string Location,
        string Url,
        bool IsExternal,
        bool OpenInNewTab,
        Guid? ParentId,
        int SortOrder,
        bool IsPublished,
        IReadOnlyList<SaveMenuTranslationRequest>? Translations)
    {
        public SaveMenuItemCommand ToCommand()
        {
            if (!TryLocation(Location, out var location))
            {
                throw new ArgumentException(
                    "Location must be Header or Footer.");
            }

            return new SaveMenuItemCommand(
                Code,
                location,
                Url,
                IsExternal,
                OpenInNewTab,
                ParentId,
                SortOrder,
                IsPublished,
                Translations?.Select(item => new SaveMenuTranslation(
                    item.LanguageCode,
                    item.Label)).ToArray() ?? []);
        }
    }
}
