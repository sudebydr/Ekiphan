using Ekiphan.Application.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Ekiphan.Api.Catalog;

internal static class AdminVariantEndpoints
{
    private const int MaximumRequestBytes = 24 * 1024;

    public static void MapAdminVariantEndpoints(
        this IEndpointRouteBuilder endpoints,
        bool authenticationConfigured)
    {
        var group = endpoints.MapGroup("/api/admin/catalog/products/{productId:guid}")
            .RequireRateLimiting("catalog-admin-read");
        if (!authenticationConfigured)
        {
            group.MapGet("/variants", UnavailableForProduct);
            group.MapPost("/variants", UnavailableForProduct);
            group.MapPut("/variants/{id:guid}", UnavailableForPair);
            group.MapPost("/variant-groups", UnavailableForProduct);
            group.MapPut("/variant-groups/{id:guid}", UnavailableForPair);
            group.MapPost(
                "/variant-groups/{id:guid}/options",
                UnavailableForPair);
            group.MapPut("/variant-options/{id:guid}", UnavailableForPair);
            return;
        }

        group.RequireAuthorization("CatalogManage");
        group.MapGet("/variants", GetAsync);
        group.MapPost("/variants", CreateVariantAsync).Write();
        group.MapPut("/variants/{id:guid}", UpdateVariantAsync).Write();
        group.MapPost("/variant-groups", CreateGroupAsync).Write();
        group.MapPut("/variant-groups/{id:guid}", UpdateGroupAsync).Write();
        group.MapPost("/variant-groups/{id:guid}/options", CreateOptionAsync).Write();
        group.MapPut("/variant-options/{id:guid}", UpdateOptionAsync).Write();
    }

    private static RouteHandlerBuilder Write(this RouteHandlerBuilder builder) =>
        builder
            .WithMetadata(new RequestSizeLimitAttribute(MaximumRequestBytes))
            .RequireRateLimiting("catalog-admin-write");

    private static async Task<IResult> GetAsync(
        Guid productId,
        HttpResponse response,
        IAdminVariantService service,
        CancellationToken cancellationToken)
    {
        NoStore(response);
        var value = await service.GetAsync(productId, cancellationToken);
        return value is null ? Results.NotFound() : Results.Ok(value);
    }

    private static Task<IResult> CreateGroupAsync(
        Guid productId,
        SaveGroupRequest request,
        HttpResponse response,
        IAdminVariantService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () => Results.Created(
            $"/api/admin/catalog/products/{productId}/variant-groups",
            await service.CreateGroupAsync(
                productId,
                request.ToCommand(),
                cancellationToken)));

    private static Task<IResult> UpdateGroupAsync(
        Guid productId,
        Guid id,
        SaveGroupRequest request,
        HttpResponse response,
        IAdminVariantService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
        {
            var value = await service.UpdateGroupAsync(
                productId,
                id,
                request.ToCommand(),
                cancellationToken);
            return value is null ? Results.NotFound() : Results.Ok(value);
        });

    private static Task<IResult> CreateOptionAsync(
        Guid productId,
        Guid id,
        SaveOptionRequest request,
        HttpResponse response,
        IAdminVariantService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () => Results.Created(
            $"/api/admin/catalog/products/{productId}/variant-options",
            await service.CreateOptionAsync(
                productId,
                id,
                request.ToCommand(),
                cancellationToken)));

    private static Task<IResult> UpdateOptionAsync(
        Guid productId,
        Guid id,
        SaveOptionRequest request,
        HttpResponse response,
        IAdminVariantService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
        {
            var value = await service.UpdateOptionAsync(
                productId,
                id,
                request.ToCommand(),
                cancellationToken);
            return value is null ? Results.NotFound() : Results.Ok(value);
        });

    private static Task<IResult> CreateVariantAsync(
        Guid productId,
        SaveVariantRequest request,
        HttpResponse response,
        IAdminVariantService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () => Results.Created(
            $"/api/admin/catalog/products/{productId}/variants",
            await service.CreateVariantAsync(
                productId,
                request.ToCommand(),
                cancellationToken)));

    private static Task<IResult> UpdateVariantAsync(
        Guid productId,
        Guid id,
        SaveVariantRequest request,
        HttpResponse response,
        IAdminVariantService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
        {
            var value = await service.UpdateVariantAsync(
                productId,
                id,
                request.ToCommand(),
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
        catch (AdminVariantConflictException exception)
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

    private static IResult UnavailableForProduct(Guid productId) =>
        Problem(
            StatusCodes.Status503ServiceUnavailable,
            "Variant administration is unavailable until JWT authentication is configured.");
    private static IResult UnavailableForPair(Guid productId, Guid id) =>
        UnavailableForProduct(productId);

    private static IResult Problem(int statusCode, string detail) =>
        Results.Problem(
            statusCode: statusCode,
            title: ReasonPhrases.GetReasonPhrase(statusCode),
            detail: detail);

    private sealed record TranslationRequest(string LanguageCode, string Name);

    private sealed record SaveGroupRequest(
        string Code,
        int SortOrder,
        IReadOnlyList<TranslationRequest>? Translations)
    {
        public SaveVariantGroupCommand ToCommand() =>
            new(Code, SortOrder, Map(Translations));
    }

    private sealed record SaveOptionRequest(
        string Code,
        int SortOrder,
        bool IsActive,
        IReadOnlyList<TranslationRequest>? Translations)
    {
        public SaveVariantOptionCommand ToCommand() =>
            new(Code, SortOrder, IsActive, Map(Translations));
    }

    private sealed record SaveVariantRequest(
        string SKU,
        int SortOrder,
        bool IsActive,
        IReadOnlyDictionary<Guid, Guid>? SelectedOptions,
        Guid? MediaAssetId)
    {
        public SaveProductVariantCommand ToCommand() =>
            new(
                SKU,
                SortOrder,
                IsActive,
                SelectedOptions ?? new Dictionary<Guid, Guid>(),
                MediaAssetId);
    }

    private static AdminVariantTranslationInput[] Map(
        IReadOnlyList<TranslationRequest>? translations) =>
        translations?.Select(item => new AdminVariantTranslationInput(
            item.LanguageCode,
            item.Name)).ToArray() ?? [];
}
