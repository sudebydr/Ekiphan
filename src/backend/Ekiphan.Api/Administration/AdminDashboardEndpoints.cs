using Ekiphan.Application.Administration;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;

namespace Ekiphan.Api.Administration;

internal static class AdminDashboardEndpoints
{
    public static void MapAdminDashboardEndpoints(
        this IEndpointRouteBuilder endpoints,
        bool authenticationConfigured)
    {
        if (!authenticationConfigured)
        {
            endpoints.MapGet("/api/admin/dashboard", Unavailable)
                .RequireRateLimiting("catalog-admin-read");
            return;
        }

        endpoints.MapGet("/api/admin/dashboard", GetAsync)
            .RequireAuthorization("AdminDashboard")
            .RequireRateLimiting("catalog-admin-read");
    }

    private static async Task<IResult> GetAsync(
        HttpResponse response,
        ClaimsPrincipal user,
        IAdminDashboardService service,
        CancellationToken cancellationToken)
    {
        response.Headers.CacheControl = "private, no-store";
        response.Headers.Pragma = "no-cache";
        var access = AdminDashboardAccess.FromPermissions(
            user.FindAll("permission").Select(claim => claim.Value));
        return Results.Ok(await service.GetAsync(access, cancellationToken));
    }

    private static IResult Unavailable() =>
        Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: ReasonPhrases.GetReasonPhrase(
                StatusCodes.Status503ServiceUnavailable),
            detail:
                "The admin dashboard is unavailable until JWT authentication is configured.");
}
