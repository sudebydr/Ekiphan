using System.Security.Claims;
using Ekiphan.Application.Identity;
using Ekiphan.Domain.Identity;

namespace Ekiphan.Api.Authentication;

internal static class AdminSecurityEndpoints
{
    public static void MapAdminSecurityEndpoints(this IEndpointRouteBuilder endpoints,bool configured)
    {
        if(!configured)return;
        var own=endpoints.MapGroup("/api/admin/security").RequireAuthorization();
        own.MapPost("/2fa/setup",Setup).WithSummary("Start TOTP two-factor setup");
        own.MapPost("/2fa/confirm",Confirm).WithSummary("Confirm TOTP and issue recovery codes once");
        own.MapPost("/2fa/disable",Disable).WithSummary("Disable two-factor authentication after step-up verification");
        own.MapPost("/2fa/recovery-codes/regenerate",Regenerate).WithSummary("Revoke and regenerate recovery codes");
        own.MapGet("/sessions",Sessions).WithSummary("List current administrator sessions");
        own.MapDelete("/sessions/{sessionId:guid}",RevokeSession).WithSummary("Revoke an owned session");
        own.MapPost("/sessions/revoke-others",RevokeOthers).WithSummary("Revoke every session except the current session");
        own.MapGet("/login-history",History).WithSummary("Get own login history");
        own.MapPost("/change-password",ChangePassword).WithSummary("Change password and revoke other sessions");
        own.MapPost("/re-authenticate",Reauthenticate).WithSummary("Issue a short-lived scoped step-up token");
        endpoints.MapGet("/api/admin/users/{userId:guid}/sessions",AdminSessions).RequireAuthorization(AdminPermissionCode.Security.SessionsRead);
        endpoints.MapPost("/api/admin/users/{userId:guid}/sessions/revoke",AdminRevoke).RequireAuthorization(AdminPermissionCode.Security.SessionsRevoke);
        endpoints.MapGet("/api/admin/security/login-attempts",LoginAttempts).RequireAuthorization(AdminPermissionCode.Security.LoginHistoryRead);
        endpoints.MapGet("/api/admin/audit-logs",AuditList).RequireAuthorization(AdminPermissionCode.Audit.Read);
        endpoints.MapGet("/api/admin/audit-logs/{id:guid}",AuditDetail).RequireAuthorization(AdminPermissionCode.Audit.Read);
        endpoints.MapGet("/api/admin/security/events",Events).RequireAuthorization(AdminPermissionCode.Security.EventsRead);
        endpoints.MapPost("/api/admin/security/events/{id:guid}/review",Review).RequireAuthorization(AdminPermissionCode.Security.EventsManage);
        endpoints.MapGet("/api/admin/security/dashboard",Dashboard).RequireAuthorization(AdminPermissionCode.Security.DashboardRead);
    }
    private static async Task<IResult>Setup(ClaimsPrincipal p,HttpContext h,IUserSecurityService s,CancellationToken ct)=>Results.Ok(await s.SetupTwoFactorAsync(new(Id(p)),Ctx(h),ct));
    private static async Task<IResult>Confirm(ConfirmRequest r,ClaimsPrincipal p,HttpContext h,IUserSecurityService s,CancellationToken ct)=>Results.Ok(await s.ConfirmTwoFactorAsync(new(Id(p),r.SetupToken,r.TotpCode),Ctx(h),ct));
    private static async Task<IResult>Disable(DisableRequest r,ClaimsPrincipal p,HttpContext h,IUserSecurityService s,CancellationToken ct){await s.DisableTwoFactorAsync(new(Id(p),r.CurrentPassword,r.Code,r.Reason),Ctx(h),ct);return Results.NoContent();}
    private static async Task<IResult>Regenerate(RegenerateRequest r,ClaimsPrincipal p,HttpContext h,IUserSecurityService s,CancellationToken ct)=>Results.Ok(new{recoveryCodes=await s.RegenerateRecoveryCodesAsync(new(Id(p),r.CurrentPassword,r.TotpCode),Ctx(h),ct)});
    private static Task<IReadOnlyList<AdminSessionDto>>Sessions(ClaimsPrincipal p,IAdminSessionService s,CancellationToken ct)=>s.GetAsync(Id(p),Session(p),ct);
    private static async Task<IResult>RevokeSession(Guid sessionId,ClaimsPrincipal p,IAdminSessionService s,CancellationToken ct)=>await s.RevokeAsync(Id(p),sessionId,false,"Revoked by owner",ct)?Results.NoContent():Results.NotFound();
    private static async Task<IResult>RevokeOthers(RevokeRequest r,ClaimsPrincipal p,IAdminSessionService s,CancellationToken ct)=>Results.Ok(new{revoked=await s.RevokeOthersAsync(Id(p),Session(p),r.Reason,ct)});
    private static Task<IReadOnlyList<LoginAttemptDto>>History(ClaimsPrincipal p,int page, int pageSize,ILoginAttemptService s,CancellationToken ct)=>s.GetUserHistoryAsync(Id(p),Math.Max(1,page),Math.Clamp(pageSize,1,100),ct);
    private static async Task<IResult>ChangePassword(ChangePasswordRequest r,ClaimsPrincipal p,HttpContext h,IPasswordSecurityService s,CancellationToken ct){await s.ChangeAsync(new(Id(p),r.CurrentPassword,r.NewPassword,r.ConfirmNewPassword,r.TotpCode,Session(p)),Ctx(h),ct);return Results.NoContent();}
    private static Task<ReAuthenticationResultDto>Reauthenticate(ReauthRequest r,ClaimsPrincipal p,IReAuthenticationService s,CancellationToken ct)=>s.ReAuthenticateAsync(new(Id(p),r.Password,r.TotpCode,r.Scope),ct);
    private static Task<IReadOnlyList<AdminSessionDto>>AdminSessions(Guid userId,ClaimsPrincipal p,IAdminSessionService s,CancellationToken ct)=>s.GetAsync(userId,Session(p),ct);
    private static async Task<IResult>AdminRevoke(Guid userId,RevokeRequest r,ClaimsPrincipal p,IAdminSessionService s,CancellationToken ct)=>Results.Ok(new{revoked=await s.RevokeUserAsync(Id(p),userId,r.Reason,ct)});
    private static Task<IReadOnlyList<LoginAttemptDto>>LoginAttempts(int page,int pageSize,ClaimsPrincipal p,ILoginAttemptService s,CancellationToken ct)=>s.GetUserHistoryAsync(Id(p),Math.Max(1,page),Math.Clamp(pageSize,1,100),ct);
    private static Task<IReadOnlyList<AuditLogListItemDto>>AuditList(int page,int pageSize,IAuditLogService s,CancellationToken ct)=>s.ListAsync(Math.Max(1,page),Math.Clamp(pageSize,1,100),ct);
    private static async Task<IResult>AuditDetail(Guid id,ClaimsPrincipal p,IAuditLogService s,CancellationToken ct)=>(await s.GetAsync(id,p.HasClaim("permission",AdminPermissionCode.Audit.DetailsRead),ct)) is{} x?Results.Ok(x):Results.NotFound();
    private static Task<IReadOnlyList<SecurityEventDto>>Events(int page,int pageSize,ISecurityEventService s,CancellationToken ct)=>s.ListAsync(Math.Max(1,page),Math.Clamp(pageSize,1,100),ct);
    private static async Task<IResult>Review(Guid id,ClaimsPrincipal p,ISecurityEventService s,CancellationToken ct)=>await s.ReviewAsync(id,Id(p),ct)?Results.NoContent():Results.NotFound();
    private static Task<SecurityDashboardDto>Dashboard(ISecurityEventService s,CancellationToken ct)=>s.DashboardAsync(ct);
    private static Guid Id(ClaimsPrincipal p)=>Guid.TryParse(p.FindFirstValue("sub"),out var x)?x:throw new UnauthorizedAccessException();private static Guid Session(ClaimsPrincipal p)=>Guid.TryParse(p.FindFirstValue("jti"),out var x)?x:throw new UnauthorizedAccessException();private static AdminSecurityContext Ctx(HttpContext h)=>new(h.Connection.RemoteIpAddress?.ToString(),h.Request.Headers.UserAgent.ToString(),h.TraceIdentifier,Session(h.User));
    private sealed record ConfirmRequest(string SetupToken,string TotpCode);private sealed record DisableRequest(string CurrentPassword,string Code,string Reason);private sealed record RegenerateRequest(string CurrentPassword,string TotpCode);private sealed record RevokeRequest(string Reason);private sealed record ChangePasswordRequest(string CurrentPassword,string NewPassword,string ConfirmNewPassword,string? TotpCode);private sealed record ReauthRequest(string Password,string? TotpCode,string Scope);
}
