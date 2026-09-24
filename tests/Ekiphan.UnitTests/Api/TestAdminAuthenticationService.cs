using Ekiphan.Application.Identity;

namespace Ekiphan.UnitTests.Api;

internal sealed class TestAdminAuthenticationService
    : IAdminAuthenticationService
{
    public const string SecurityStamp =
        "0123456789abcdef0123456789abcdef";

    public Task<AdminAuthenticatedUser?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AdminAuthenticatedUser?>(null);

    public Task CreateSessionAsync(
        Guid userId,
        Guid sessionId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<AdminAuthenticatedUser?> ValidateSessionAsync(
        Guid userId,
        Guid sessionId,
        string securityStamp,
        TimeSpan idleTimeout,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AdminAuthenticatedUser?>(
            userId == Guid.Empty ||
            sessionId == Guid.Empty ||
            !string.Equals(
                securityStamp,
                SecurityStamp,
                StringComparison.Ordinal)
                ? null
                : new AdminAuthenticatedUser(
                    userId,
                    "test-admin@example.com",
                    "Test Administrator",
                    [],
                    SecurityStamp));

    public Task<bool> RevokeSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(userId != Guid.Empty && sessionId != Guid.Empty);
}
