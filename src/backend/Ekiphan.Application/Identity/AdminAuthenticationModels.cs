namespace Ekiphan.Application.Identity;

public sealed record AdminAuthenticatedUser(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Permissions,
    string SecurityStamp,
    bool IsTwoFactorEnabled=false,
    long AuthorizationVersion=1);

public interface IAdminAuthenticationService
{
    Task<AdminAuthenticatedUser?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task CreateSessionAsync(
        Guid userId,
        Guid sessionId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);

    Task<AdminAuthenticatedUser?> ValidateSessionAsync(
        Guid userId,
        Guid sessionId,
        string securityStamp,
        TimeSpan idleTimeout,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default);
}
