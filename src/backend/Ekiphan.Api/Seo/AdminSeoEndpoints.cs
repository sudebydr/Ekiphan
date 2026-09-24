using System.Security.Claims;
using Ekiphan.Application.Seo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Ekiphan.Api.Seo;

internal static class AdminSeoEndpoints
{
    public static void MapAdminSeoEndpoints(this IEndpointRouteBuilder endpoints, bool authenticationConfigured)
    {
        var group = endpoints.MapGroup("/api/admin/seo")
            .RequireRateLimiting("catalog-admin-read");
        if (!authenticationConfigured)
        {
            group.MapGet("", Unavailable);
            group.MapGet("/summary", Unavailable);
            return;
        }
        group.RequireAuthorization("SeoAccess");
        group.MapGet("", ListAsync);
        group.MapGet("/summary", SummaryAsync);
        group.MapGet("/duplicates", DuplicatesAsync);
        group.MapGet("/slug-availability", SlugAsync);
        group.MapGet("/{type}/{id:guid}", DetailAsync);
        group.MapPut("/{type}/{id:guid}", UpdateAsync)
            .RequireRateLimiting("catalog-admin-write")
            .WithMetadata(new RequestSizeLimitAttribute(64 * 1024));
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] SeoQueryParameters values, ClaimsPrincipal user,
        IAdminSeoService service, CancellationToken ct) =>
        await ExecuteAsync(() => service.GetPageAsync(values.ToQuery(), Scope(user), ct));

    private static async Task<IResult> SummaryAsync(
        ClaimsPrincipal user, IAdminSeoService service, CancellationToken ct) =>
        await ExecuteAsync(() => service.GetHealthAsync(Scope(user), ct));

    private static async Task<IResult> DuplicatesAsync(
        ClaimsPrincipal user, IAdminSeoService service, CancellationToken ct) =>
        await ExecuteAsync(() => service.GetDuplicatesAsync(Scope(user), ct));

    private static async Task<IResult> DetailAsync(
        string type, Guid id, ClaimsPrincipal user, IAdminSeoService service, CancellationToken ct)
    {
        if (!TryType(type, out var parsed)) return Problem(400, "Content type is invalid.");
        return await ExecuteAsync(async () => await service.GetDetailAsync(parsed, id, Scope(user), ct), true);
    }

    private static async Task<IResult> UpdateAsync(
        string type, Guid id, UpdateAdminSeoCommand request, ClaimsPrincipal user,
        IAdminSeoService service, CancellationToken ct)
    {
        if (!TryType(type, out var parsed)) return Problem(400, "Content type is invalid.");
        return await ExecuteAsync(async () => await service.UpdateAsync(parsed, id, request, Scope(user), ct), true);
    }

    private static async Task<IResult> SlugAsync(
        string type, Guid? id, string language, string slug, ClaimsPrincipal user,
        IAdminSeoService service, CancellationToken ct)
    {
        if (!TryType(type, out var parsed)) return Problem(400, "Content type is invalid.");
        return await ExecuteAsync(() => service.IsSlugAvailableAsync(parsed, id, language, slug, Scope(user), ct));
    }

    private static async Task<IResult> ExecuteAsync<T>(Func<Task<T>> action, bool notFoundWhenNull = false)
    {
        try
        {
            var value = await action();
            return notFoundWhenNull && value is null ? Results.NotFound() : Results.Ok(value);
        }
        catch (UnauthorizedAccessException) { return Problem(403, "You do not have permission for this content type."); }
        catch (AdminSeoConflictException ex) { return Problem(409, ex.Message); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { return Problem(400, ex.Message); }
    }

    private static AdminSeoAccessScope Scope(ClaimsPrincipal user) => new(
        user.HasClaim("permission", "catalog.manage"),
        user.HasClaim("permission", "content.manage"));
    private static bool TryType(string value, out SeoContentType type) =>
        Enum.TryParse(value, true, out type) && Enum.IsDefined(type);
    private static IResult Unavailable() => Problem(503, "SEO administration is unavailable until JWT authentication is configured.");
    private static IResult Problem(int status, string detail) => Results.Problem(statusCode: status, title: ReasonPhrases.GetReasonPhrase(status), detail: detail);

    internal sealed record SeoQueryParameters(
        SeoContentType? ContentType, string? Language, bool? Published,
        bool? MissingMetaTitle, bool? MissingMetaDescription,
        bool? MissingOpenGraphImage, bool? DuplicateSlug,
        bool? DuplicateMetaTitle, bool? NoIndex, string? Search,
        string? Sort, int? Page, int? PageSize)
    {
        public AdminSeoQuery ToQuery() => new(ContentType, Language, Published,
            MissingMetaTitle, MissingMetaDescription, MissingOpenGraphImage,
            DuplicateSlug, DuplicateMetaTitle, NoIndex, Search,
            Sort ?? "updated-desc", Page ?? 1, PageSize ?? 25);
    }
}
