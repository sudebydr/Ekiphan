using Ekiphan.Application.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Ekiphan.Api.Catalog;

internal static class AdminCategoryEndpoints
{
    private const int MaximumRequestBytes = 24 * 1024;

    public static void MapAdminCategoryEndpoints(
        this IEndpointRouteBuilder endpoints,
        bool authenticationConfigured)
    {
        var group = endpoints.MapGroup("/api/admin/catalog")
            .RequireRateLimiting("catalog-admin-read");
        if (!authenticationConfigured)
        {
            group.MapGet("/structure", AuthenticationUnavailable);
            group.MapPost("/sections", AuthenticationUnavailable)
                .WithMetadata(new RequestSizeLimitAttribute(MaximumRequestBytes));
            group.MapPut("/sections/{id:guid}", AuthenticationUnavailableForId)
                .WithMetadata(new RequestSizeLimitAttribute(MaximumRequestBytes));
            group.MapPost("/categories", AuthenticationUnavailable)
                .WithMetadata(new RequestSizeLimitAttribute(MaximumRequestBytes));
            group.MapPut("/categories/{id:guid}", AuthenticationUnavailableForId)
                .WithMetadata(new RequestSizeLimitAttribute(MaximumRequestBytes));
            group.MapDelete(
                "/categories/{id:guid}",
                AuthenticationUnavailableForId);
            return;
        }

        group.RequireAuthorization("CatalogManage");
        group.MapGet("/structure", GetAsync);
        group.MapPost("/sections", CreateSectionAsync)
            .WithMetadata(new RequestSizeLimitAttribute(MaximumRequestBytes))
            .RequireRateLimiting("catalog-admin-write");
        group.MapPut("/sections/{id:guid}", UpdateSectionAsync)
            .WithMetadata(new RequestSizeLimitAttribute(MaximumRequestBytes))
            .RequireRateLimiting("catalog-admin-write");
        group.MapPost("/categories", CreateCategoryAsync)
            .WithMetadata(new RequestSizeLimitAttribute(MaximumRequestBytes))
            .RequireRateLimiting("catalog-admin-write");
        group.MapPut("/categories/{id:guid}", UpdateCategoryAsync)
            .WithMetadata(new RequestSizeLimitAttribute(MaximumRequestBytes))
            .RequireRateLimiting("catalog-admin-write");
        group.MapDelete("/categories/{id:guid}", ArchiveCategoryAsync)
            .RequireRateLimiting("catalog-admin-write");
    }

    private static async Task<IResult> GetAsync(
        HttpResponse response,
        IAdminCategoryService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        return Results.Ok(await service.GetAsync(cancellationToken));
    }

    private static async Task<IResult> CreateSectionAsync(
        SaveSectionRequest request,
        HttpResponse response,
        IAdminCategoryService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        try
        {
            var result = await service.CreateSectionAsync(
                request.ToCommand(),
                cancellationToken);
            return Results.Created(
                $"/api/admin/catalog/sections/{result.Id}",
                result);
        }
        catch (Exception exception) when (
            exception is ArgumentException or AdminCategoryConflictException)
        {
            return Failure(exception);
        }
    }

    private static async Task<IResult> UpdateSectionAsync(
        Guid id,
        SaveSectionRequest request,
        HttpResponse response,
        IAdminCategoryService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        try
        {
            var result = await service.UpdateSectionAsync(
                id,
                request.ToCommand(),
                cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }
        catch (Exception exception) when (
            exception is ArgumentException or AdminCategoryConflictException)
        {
            return Failure(exception);
        }
    }

    private static async Task<IResult> CreateCategoryAsync(
        SaveCategoryRequest request,
        HttpResponse response,
        IAdminCategoryService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        try
        {
            var result = await service.CreateCategoryAsync(
                request.ToCommand(),
                cancellationToken);
            return Results.Created(
                $"/api/admin/catalog/categories/{result.Id}",
                result);
        }
        catch (Exception exception) when (
            exception is ArgumentException or AdminCategoryConflictException)
        {
            return Failure(exception);
        }
    }

    private static async Task<IResult> UpdateCategoryAsync(
        Guid id,
        SaveCategoryRequest request,
        HttpResponse response,
        IAdminCategoryService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        try
        {
            var result = await service.UpdateCategoryAsync(
                id,
                request.ToCommand(),
                cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }
        catch (Exception exception) when (
            exception is ArgumentException or AdminCategoryConflictException)
        {
            return Failure(exception);
        }
    }

    private static async Task<IResult> ArchiveCategoryAsync(
        Guid id,
        HttpResponse response,
        IAdminCategoryService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        try
        {
            return await service.ArchiveCategoryAsync(id, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound();
        }
        catch (AdminCategoryConflictException exception)
        {
            return Failure(exception);
        }
    }

    private static IResult Failure(Exception exception) =>
        Problem(
            exception is AdminCategoryConflictException
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest,
            exception.Message);

    private static void SetPrivateNoStore(HttpResponse response)
    {
        response.Headers.CacheControl = "private, no-store";
        response.Headers.Pragma = "no-cache";
    }

    private static IResult AuthenticationUnavailable() =>
        Problem(
            StatusCodes.Status503ServiceUnavailable,
            "Category administration is unavailable until JWT authentication is configured.");

    private static IResult AuthenticationUnavailableForId(Guid id) =>
        AuthenticationUnavailable();

    private static IResult Problem(int statusCode, string detail) =>
        Results.Problem(
            statusCode: statusCode,
            title: ReasonPhrases.GetReasonPhrase(statusCode),
            detail: detail);

    private sealed record TranslationRequest(
        string LanguageCode,
        string Name,
        string Slug,
        string? Description,
        string? MetaTitle,
        string? MetaDescription,
        string? CanonicalUrl,
        bool NoIndex,
        bool NoFollow,
        string? OpenGraphTitle,
        string? OpenGraphDescription,
        Guid? OpenGraphImageMediaId);

    private sealed record SaveSectionRequest(
        string Code,
        int SortOrder,
        bool IsPublished,
        IReadOnlyList<TranslationRequest>? Translations)
    {
        public SaveAdminSectionCommand ToCommand() =>
            new(
                Code,
                SortOrder,
                IsPublished,
                Translations?.Select(item => new AdminSectionTranslationInput(
                    item.LanguageCode,
                    item.Name,
                    item.Slug)).ToArray() ?? []);
    }

    private sealed record SaveCategoryRequest(
        Guid ProductSectionId,
        Guid? ParentId,
        int SortOrder,
        bool IsPublished,
        IReadOnlyList<TranslationRequest>? Translations)
    {
        public SaveAdminCategoryCommand ToCommand() =>
            new(
                ProductSectionId,
                ParentId,
                SortOrder,
                IsPublished,
                Translations?.Select(item => new AdminCategoryTranslationInput(
                    item.LanguageCode,
                    item.Name,
                    item.Slug,
                    item.Description,
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
