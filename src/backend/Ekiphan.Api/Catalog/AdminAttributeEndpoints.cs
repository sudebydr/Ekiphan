using Ekiphan.Application.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Ekiphan.Api.Catalog;

internal static class AdminAttributeEndpoints
{
    private const int MaximumRequestBytes = 24 * 1024;

    public static void MapAdminAttributeEndpoints(
        this IEndpointRouteBuilder endpoints,
        bool authenticationConfigured)
    {
        var group = endpoints.MapGroup("/api/admin/catalog")
            .RequireRateLimiting("catalog-admin-read");
        if (!authenticationConfigured)
        {
            group.MapGet("/attributes", Unavailable);
            group.MapPost("/attributes", Unavailable);
            group.MapPut("/attributes/{id:guid}", UnavailableForId);
            group.MapPost("/attributes/{id:guid}/options", UnavailableForId);
            group.MapPut("/attribute-options/{id:guid}", UnavailableForId);
            group.MapPut("/category-attributes", Unavailable);
            group.MapDelete(
                "/category-attributes/{categoryId:guid}/{attributeId:guid}",
                UnavailableForPair);
            return;
        }

        group.RequireAuthorization("CatalogManage");
        group.MapGet("/attributes", GetAsync);
        group.MapPost("/attributes", CreateAsync).Write();
        group.MapPut("/attributes/{id:guid}", UpdateAsync).Write();
        group.MapPost("/attributes/{id:guid}/options", CreateOptionAsync).Write();
        group.MapPut("/attribute-options/{id:guid}", UpdateOptionAsync).Write();
        group.MapPut("/category-attributes", SaveAssignmentAsync).Write();
        group.MapDelete(
            "/category-attributes/{categoryId:guid}/{attributeId:guid}",
            RemoveAssignmentAsync).RequireRateLimiting("catalog-admin-write");
    }

    private static RouteHandlerBuilder Write(this RouteHandlerBuilder builder) =>
        builder
            .WithMetadata(new RequestSizeLimitAttribute(MaximumRequestBytes))
            .RequireRateLimiting("catalog-admin-write");

    private static async Task<IResult> GetAsync(
        HttpResponse response,
        IAdminAttributeService service,
        CancellationToken cancellationToken)
    {
        NoStore(response);
        return Results.Ok(await service.GetAsync(cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        SaveAttributeRequest request,
        HttpResponse response,
        IAdminAttributeService service,
        CancellationToken cancellationToken) =>
        await Execute(
            response,
            async () =>
            {
                var value = await service.CreateAsync(
                    request.ToCommand(),
                    cancellationToken);
                return Results.Created(
                    $"/api/admin/catalog/attributes/{value.Id}",
                    value);
            });

    private static async Task<IResult> UpdateAsync(
        Guid id,
        SaveAttributeRequest request,
        HttpResponse response,
        IAdminAttributeService service,
        CancellationToken cancellationToken) =>
        await Execute(
            response,
            async () =>
            {
                var value = await service.UpdateAsync(
                    id,
                    request.ToCommand(),
                    cancellationToken);
                return value is null ? Results.NotFound() : Results.Ok(value);
            });

    private static async Task<IResult> CreateOptionAsync(
        Guid id,
        SaveOptionRequest request,
        HttpResponse response,
        IAdminAttributeService service,
        CancellationToken cancellationToken) =>
        await Execute(
            response,
            async () =>
            {
                var value = await service.CreateOptionAsync(
                    id,
                    request.ToCommand(),
                    cancellationToken);
                return Results.Created(
                    $"/api/admin/catalog/attribute-options/{value.Id}",
                    value);
            });

    private static async Task<IResult> UpdateOptionAsync(
        Guid id,
        SaveOptionRequest request,
        HttpResponse response,
        IAdminAttributeService service,
        CancellationToken cancellationToken) =>
        await Execute(
            response,
            async () =>
            {
                var value = await service.UpdateOptionAsync(
                    id,
                    request.ToCommand(),
                    cancellationToken);
                return value is null ? Results.NotFound() : Results.Ok(value);
            });

    private static async Task<IResult> SaveAssignmentAsync(
        SaveAssignmentRequest request,
        HttpResponse response,
        IAdminAttributeService service,
        CancellationToken cancellationToken) =>
        await Execute(
            response,
            async () => Results.Ok(
                await service.SaveAssignmentAsync(
                    request.ToCommand(),
                    cancellationToken)));

    private static async Task<IResult> RemoveAssignmentAsync(
        Guid categoryId,
        Guid attributeId,
        HttpResponse response,
        IAdminAttributeService service,
        CancellationToken cancellationToken)
    {
        NoStore(response);
        return await service.RemoveAssignmentAsync(
            categoryId,
            attributeId,
            cancellationToken)
                ? Results.NoContent()
                : Results.NotFound();
    }

    private static async Task<IResult> Execute(
        HttpResponse response,
        Func<Task<IResult>> operation)
    {
        NoStore(response);
        try
        {
            return await operation();
        }
        catch (AdminAttributeConflictException exception)
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
            "Attribute administration is unavailable until JWT authentication is configured.");

    private static IResult UnavailableForId(Guid id) => Unavailable();
    private static IResult UnavailableForPair(Guid categoryId, Guid attributeId) =>
        Unavailable();

    private static IResult Problem(int statusCode, string detail) =>
        Results.Problem(
            statusCode: statusCode,
            title: ReasonPhrases.GetReasonPhrase(statusCode),
            detail: detail);

    private sealed record TranslationRequest(string LanguageCode, string Name);

    private sealed record SaveAttributeRequest(
        string Code,
        string DataType,
        string? UnitDimension,
        bool IsActive,
        IReadOnlyList<TranslationRequest>? Translations)
    {
        public SaveAdminAttributeCommand ToCommand() =>
            new(
                Code,
                DataType,
                UnitDimension,
                IsActive,
                Translations?.Select(item => new AdminAttributeTranslationInput(
                    item.LanguageCode,
                    item.Name)).ToArray() ?? []);
    }

    private sealed record SaveOptionRequest(
        string Code,
        int SortOrder,
        bool IsActive,
        IReadOnlyList<TranslationRequest>? Translations)
    {
        public AdminAttributeOptionInput ToCommand() =>
            new(
                Code,
                SortOrder,
                IsActive,
                Translations?.Select(item => new AdminAttributeTranslationInput(
                    item.LanguageCode,
                    item.Name)).ToArray() ?? []);
    }

    private sealed record SaveAssignmentRequest(
        Guid CategoryId,
        Guid AttributeId,
        bool IsRequired,
        bool IsFilterable,
        bool IsVisibleOnProduct,
        bool IsVisibleOnComparison,
        int SortOrder)
    {
        public SaveCategoryAttributeCommand ToCommand() =>
            new(
                CategoryId,
                AttributeId,
                IsRequired,
                IsFilterable,
                IsVisibleOnProduct,
                IsVisibleOnComparison,
                SortOrder);
    }
}
