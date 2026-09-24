using System.Security.Claims;
using System.Text;
using Ekiphan.Application.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ekiphan.Api.Authentication;

internal static class AdminAuthenticationEndpoints
{
    internal const string AuthenticatedUserItemKey =
        "Ekiphan.AdminAuthenticatedUser";

    public static void MapAdminAuthenticationEndpoints(
        this IEndpointRouteBuilder endpoints,
        JwtSettings settings)
    {
        if (!settings.IsConfigured)
        {
            endpoints.MapPost("/api/admin/auth/login", Unavailable)
                .RequireRateLimiting("admin-login");
            endpoints.MapGet("/api/admin/auth/session", Unavailable)
                .RequireRateLimiting("catalog-admin-read");
            endpoints.MapPost("/api/admin/auth/logout", Unavailable)
                .RequireRateLimiting("catalog-admin-write");
            return;
        }

        endpoints.MapPost(
                "/api/admin/auth/login",
                (
                    LoginRequest request,
                    IAdminAuthenticationService service,
                    IUserSecurityService securityService,
                    HttpContext context,
                    CancellationToken cancellationToken) =>
                    LoginAsync(
                        request,
                        service,
                        securityService,
                        context,
                        settings,
                        cancellationToken))
            .WithMetadata(new RequestSizeLimitAttribute(8 * 1024))
            .RequireRateLimiting("admin-login");
        endpoints.MapPost("/api/admin/auth/verify-two-factor",(VerifyTwoFactorRequest request,HttpContext context,IUserSecurityService security,IAdminAuthenticationService auth,CancellationToken ct)=>VerifyTwoFactorAsync(request,context,security,auth,settings,ct))
            .WithMetadata(new RequestSizeLimitAttribute(8*1024)).RequireRateLimiting("admin-login");
        endpoints.MapPost("/api/admin/auth/forgot-password",ForgotPasswordAsync)
            .WithMetadata(new RequestSizeLimitAttribute(8*1024)).RequireRateLimiting("admin-login");
        endpoints.MapPost("/api/admin/auth/reset-password",ResetPasswordAsync)
            .WithMetadata(new RequestSizeLimitAttribute(8*1024)).RequireRateLimiting("admin-login");
        endpoints.MapGet(
                "/api/admin/auth/session",
                (HttpContext context) => GetSession(context, settings))
            .RequireAuthorization("AdminDashboard")
            .RequireRateLimiting("catalog-admin-read");
        endpoints.MapPost("/api/admin/auth/logout", LogoutAsync)
            .RequireAuthorization()
            .RequireRateLimiting("catalog-admin-write");
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IAdminAuthenticationService service,
        IUserSecurityService securityService,
        HttpContext context,
        JwtSettings settings,
        CancellationToken cancellationToken)
    {
        var user = await service.AuthenticateAsync(
            request.Email ?? string.Empty,
            request.Password ?? string.Empty,
            cancellationToken);
        if (user is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication failed",
                detail: "Email or password is invalid.");
        }

        if(user.IsTwoFactorEnabled)
        {
            var challenge=await securityService.CreateLoginChallengeAsync(user.Id,cancellationToken);
            return Results.Ok(new {requiresTwoFactor=true,twoFactorToken=challenge.TwoFactorToken,expiresAt=challenge.ExpiresAt,allowedMethods=challenge.AllowedMethods});
        }


        return await IssueTokenAsync(user,service,settings,cancellationToken);
    }

    private static async Task<IResult> VerifyTwoFactorAsync(VerifyTwoFactorRequest request,HttpContext context,IUserSecurityService securityService,IAdminAuthenticationService auth,JwtSettings settings,CancellationToken ct)
    {
        try
        {
            var result=await securityService.VerifyLoginAsync(new(request.TwoFactorToken??"",request.Code??"",request.Method??""),Context(context),ct);
            return await IssueTokenAsync(result.User,auth,settings,ct);
        }
        catch(InvalidOperationException){return Results.Problem(statusCode:401,title:"Two-factor verification failed",detail:"Verification token or code is invalid.");}
    }

    private static async Task<IResult> IssueTokenAsync(AdminAuthenticatedUser user,IAdminAuthenticationService service,JwtSettings settings,CancellationToken cancellationToken)
    {

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(
            settings.AccessTokenMinutes);
        var sessionId = Guid.NewGuid();
        await service.CreateSessionAsync(
            user.Id,
            sessionId,
            expiresAt,
            cancellationToken);
        var claims = new Dictionary<string, object>
        {
            ["sub"] = user.Id.ToString(),
            ["email"] = user.Email,
            ["name"] = user.DisplayName,
            ["jti"] = sessionId.ToString("N"),
            ["security_stamp"] = user.SecurityStamp,
            ["authorization_version"] = user.AuthorizationVersion,
            ["permission"] = user.Permissions.ToArray(),
        };
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Expires = expiresAt.UtcDateTime,
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(settings.SigningKey!)),
                SecurityAlgorithms.HmacSha256),
        };
        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return Results.Ok(
            new LoginResponse(
                token,
                expiresAt,
                user.DisplayName,
                user.Permissions));
    }

    private static async Task<IResult> ForgotPasswordAsync(ForgotPasswordRequest request,HttpContext context,IPasswordResetService service,CancellationToken ct)
    {await service.RequestAsync(new(request.Email??string.Empty),Context(context),ct);return Results.Accepted(value:new{message="Hesap bulunuyorsa parola sıfırlama bağlantısı gönderildi."});}

    private static async Task<IResult> ResetPasswordAsync(ResetPasswordRequest request,HttpContext context,IPasswordResetService service,CancellationToken ct)
    {try{await service.ResetAsync(new(request.Token??string.Empty,request.NewPassword??string.Empty,request.ConfirmNewPassword??string.Empty),Context(context),ct);return Results.NoContent();}catch(InvalidOperationException){return Results.Problem(statusCode:400,title:"Password reset failed",detail:"Reset token is invalid or expired.");}}

    private static AdminSecurityContext Context(HttpContext c)=>new(c.Connection.RemoteIpAddress?.ToString(),c.Request.Headers.UserAgent.ToString(),c.TraceIdentifier);

    private static IResult Unavailable() =>
        Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Authentication unavailable",
            detail:
                "Admin authentication is unavailable until JWT settings are configured.");

    private static IResult GetSession(
        HttpContext context,
        JwtSettings settings)
    {
        context.Response.Headers.CacheControl = "private, no-store";
        context.Response.Headers.Pragma = "no-cache";
        if (!context.Items.TryGetValue(
                AuthenticatedUserItemKey,
                out var authenticatedUser) ||
            authenticatedUser is not AdminAuthenticatedUser user)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(
            new AdminSessionResponse(
                true,
                settings.IdleTimeoutMinutes,
                new AdminSessionUserResponse(
                    user.Id,
                    user.Email,
                    user.DisplayName,
                    [],
                    user.Permissions)));
    }

    private static async Task<IResult> LogoutAsync(
        ClaimsPrincipal principal,
        HttpResponse response,
        IAdminAuthenticationService service,
        CancellationToken cancellationToken)
    {
        response.Headers.CacheControl = "private, no-store";
        response.Headers.Pragma = "no-cache";
        if (!Guid.TryParse(principal.FindFirstValue("sub"), out var userId) ||
            !Guid.TryParse(principal.FindFirstValue("jti"), out var sessionId))
        {
            return Results.Unauthorized();
        }

        return await service.RevokeSessionAsync(
                userId,
                sessionId,
                cancellationToken)
            ? Results.Ok(new LogoutResponse(true))
            : Results.Unauthorized();
    }

    private sealed record LoginRequest(string? Email, string? Password);
    private sealed record VerifyTwoFactorRequest(string? TwoFactorToken,string? Code,string? Method);
    private sealed record ForgotPasswordRequest(string? Email);
    private sealed record ResetPasswordRequest(string? Token,string? NewPassword,string? ConfirmNewPassword);

    private sealed record LoginResponse(
        string AccessToken,
        DateTimeOffset ExpiresAt,
        string DisplayName,
        IReadOnlyList<string> Permissions);

    private sealed record AdminSessionResponse(
        bool Authenticated,
        int IdleTimeoutMinutes,
        AdminSessionUserResponse User);

    private sealed record LogoutResponse(bool SignedOut);

    private sealed record AdminSessionUserResponse(
        Guid Id,
        string Email,
        string DisplayName,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Permissions);
}
