using Ekiphan.Application.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Ekiphan.Api.Authentication;

internal static class AdminUserEndpoints
{
    public static void MapAdminUserEndpoints(
        this IEndpointRouteBuilder endpoints,
        bool authenticationConfigured)
    {
        var group = endpoints.MapGroup("/api/admin/users")
            .RequireRateLimiting("catalog-admin-read");
        if (!authenticationConfigured)
        {
            group.MapGet("", Unavailable);
            group.MapPost("", Unavailable);
            group.MapPut("/{id:guid}", UnavailableForId);
            group.MapPut("/{id:guid}/password", UnavailableForId);
            return;
        }

        group.RequireAuthorization("UserManage");
        group.MapGet("", GetAsync);
        group.MapPost("", CreateAsync).Write(24 * 1024);
        group.MapPut("/{id:guid}", UpdateAsync).Write(24 * 1024);
        group.MapPut("/{id:guid}/password", ResetPasswordAsync)
            .Write(8 * 1024);
    }

    private static async Task<IResult> GetAsync(
        HttpRequest request,
        HttpResponse response,
        IAdminUserManagementService service,
        CancellationToken cancellationToken)
    {
        NoStore(response);
        if (!TryPage(request, out var page, out var pageSize))
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "Invalid paging values.");
        }

        var search = request.Query.TryGetValue("search", out var rawSearch)
            ? rawSearch.ToString().Trim()
            : null;
        if (search is { Length: > 100 })
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "search cannot exceed 100 characters.");
        }

        return Results.Ok(
            await service.GetUsersAsync(
                new AdminUserListQuery(page, pageSize, search),
                cancellationToken));
    }

    private static bool TryPage(
        HttpRequest request,
        out int page,
        out int pageSize)
    {
        page = 1;
        pageSize = 50;
        return (!request.Query.TryGetValue("page", out var rawPage) ||
                int.TryParse(rawPage, out page)) &&
            (!request.Query.TryGetValue("pageSize", out var rawPageSize) ||
             int.TryParse(rawPageSize, out pageSize)) &&
            page >= 1 &&
            pageSize is >= 1 and <= 100;
    }

    private static Task<IResult> CreateAsync(
        SaveUserRequest request,
        HttpResponse response,
        IAdminUserManagementService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
        {
            var value = await service.CreateAsync(
                new CreateAdminUserCommand(
                    request.Email ?? string.Empty,
                    request.DisplayName ?? string.Empty,
                    request.Password ?? string.Empty,
                    request.IsActive,
                    request.Permissions ?? []),
                cancellationToken);
            return Results.Created(
                $"/api/admin/users/{value.Id}",
                value);
        });

    private static Task<IResult> UpdateAsync(
        Guid id,
        SaveUserRequest request,
        HttpResponse response,
        IAdminUserManagementService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
        {
            var value = await service.UpdateAsync(
                id,
                new UpdateAdminUserCommand(
                    request.Email ?? string.Empty,
                    request.DisplayName ?? string.Empty,
                    request.IsActive,
                    request.Permissions ?? []),
                cancellationToken);
            return value is null ? Results.NotFound() : Results.Ok(value);
        });

    private static Task<IResult> ResetPasswordAsync(
        Guid id,
        ResetPasswordRequest request,
        HttpResponse response,
        IAdminUserManagementService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
            await service.ResetPasswordAsync(
                id,
                request.Password ?? string.Empty,
                cancellationToken)
                ? Results.NoContent()
                : Results.NotFound());

    private static RouteHandlerBuilder Write(
        this RouteHandlerBuilder builder,
        int maximumBytes) =>
        builder
            .WithMetadata(new RequestSizeLimitAttribute(maximumBytes))
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
        catch (AdminUserConflictException exception)
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
            "User administration is unavailable until JWT authentication is configured.");

    private static IResult UnavailableForId(Guid id) => Unavailable();

    private static IResult Problem(int statusCode, string detail) =>
        Results.Problem(
            statusCode: statusCode,
            title: ReasonPhrases.GetReasonPhrase(statusCode),
            detail: detail);

    private sealed record SaveUserRequest(
        string? Email,
        string? DisplayName,
        string? Password,
        bool IsActive,
        IReadOnlyList<string>? Permissions);

    private sealed record ResetPasswordRequest(string? Password);
}
