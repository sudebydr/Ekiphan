using Ekiphan.Application.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Ekiphan.Api.Catalog;

internal static class AdminDictionaryEndpoints
{
    public static void MapAdminDictionaryEndpoints(
        this IEndpointRouteBuilder endpoints,
        bool authenticationConfigured)
    {
        var group = endpoints.MapGroup("/api/admin/catalog")
            .RequireRateLimiting("catalog-admin-read");
        if (!authenticationConfigured)
        {
            group.MapGet("/dictionaries", Unavailable);
            group.MapPost("/tags", Unavailable);
            group.MapPut("/tags/{id:guid}", UnavailableForId);
            group.MapPost("/units", Unavailable);
            group.MapPut("/units/{id:guid}", UnavailableForId);
            group.MapPut("/products/{id:guid}/tags", UnavailableForId);
            return;
        }

        group.RequireAuthorization("CatalogManage");
        group.MapGet("/dictionaries", GetAsync);
        group.MapPost("/tags", CreateTagAsync).Write();
        group.MapPut("/tags/{id:guid}", UpdateTagAsync).Write();
        group.MapPost("/units", CreateUnitAsync).Write();
        group.MapPut("/units/{id:guid}", UpdateUnitAsync).Write();
        group.MapPut("/products/{id:guid}/tags", SaveProductTagsAsync).Write();
    }

    private static RouteHandlerBuilder Write(this RouteHandlerBuilder builder) =>
        builder
            .WithMetadata(new RequestSizeLimitAttribute(24 * 1024))
            .RequireRateLimiting("catalog-admin-write");

    private static async Task<IResult> GetAsync(
        HttpResponse response,
        IAdminDictionaryService service,
        CancellationToken cancellationToken)
    {
        NoStore(response);
        return Results.Ok(await service.GetAsync(cancellationToken));
    }

    private static Task<IResult> CreateTagAsync(
        SaveTagRequest request,
        HttpResponse response,
        IAdminDictionaryService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
        {
            var value = await service.CreateTagAsync(
                request.ToCommand(),
                cancellationToken);
            return Results.Created(
                $"/api/admin/catalog/tags/{value.Id}",
                value);
        });

    private static Task<IResult> UpdateTagAsync(
        Guid id,
        SaveTagRequest request,
        HttpResponse response,
        IAdminDictionaryService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
        {
            var value = await service.UpdateTagAsync(
                id,
                request.ToCommand(),
                cancellationToken);
            return value is null ? Results.NotFound() : Results.Ok(value);
        });

    private static Task<IResult> CreateUnitAsync(
        SaveUnitRequest request,
        HttpResponse response,
        IAdminDictionaryService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
        {
            var value = await service.CreateUnitAsync(
                request.ToCommand(),
                cancellationToken);
            return Results.Created(
                $"/api/admin/catalog/units/{value.Id}",
                value);
        });

    private static Task<IResult> UpdateUnitAsync(
        Guid id,
        SaveUnitRequest request,
        HttpResponse response,
        IAdminDictionaryService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
        {
            var value = await service.UpdateUnitAsync(
                id,
                request.ToCommand(),
                cancellationToken);
            return value is null ? Results.NotFound() : Results.Ok(value);
        });

    private static Task<IResult> SaveProductTagsAsync(
        Guid id,
        SaveProductTagsRequest request,
        HttpResponse response,
        IAdminDictionaryService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
        {
            var value = await service.SaveProductTagsAsync(
                id,
                request.TagIds ?? [],
                cancellationToken);
            return value is null ? Results.NotFound() : Results.Ok(value);
        });

    private static async Task<IResult> Execute(
        HttpResponse response,
        Func<Task<IResult>> operation)
    {
        NoStore(response);
        try
        {
            return await operation();
        }
        catch (AdminDictionaryConflictException exception)
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
            "Dictionary administration is unavailable until JWT authentication is configured.");
    private static IResult UnavailableForId(Guid id) => Unavailable();

    private static IResult Problem(int statusCode, string detail) =>
        Results.Problem(
            statusCode: statusCode,
            title: ReasonPhrases.GetReasonPhrase(statusCode),
            detail: detail);

    private sealed record TranslationRequest(
        string LanguageCode,
        string Name,
        string Slug);

    private sealed record SaveTagRequest(
        string Code,
        bool IsActive,
        IReadOnlyList<TranslationRequest>? Translations)
    {
        public SaveAdminTagCommand ToCommand() =>
            new(
                Code,
                IsActive,
                Translations?.Select(item => new AdminTagTranslationInput(
                    item.LanguageCode,
                    item.Name,
                    item.Slug)).ToArray() ?? []);
    }

    private sealed record SaveUnitRequest(
        string Code,
        string Symbol,
        string Dimension,
        decimal ConversionFactorToBase,
        bool IsBaseUnit,
        bool IsActive)
    {
        public SaveAdminUnitCommand ToCommand() =>
            new(
                Code,
                Symbol,
                Dimension,
                ConversionFactorToBase,
                IsBaseUnit,
                IsActive);
    }

    private sealed record SaveProductTagsRequest(
        IReadOnlyList<Guid>? TagIds);
}
