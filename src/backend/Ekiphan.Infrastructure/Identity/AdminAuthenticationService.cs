using Ekiphan.Application.Identity;
using Ekiphan.Domain.Identity;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Identity;

internal sealed class AdminAuthenticationService(
    EkiphanDbContext dbContext,
    IPasswordHasher<AdminUser> passwordHasher,
    IUserPermissionService permissionService,
    TimeProvider timeProvider)
    : IAdminAuthenticationService
{
    public async Task<AdminAuthenticatedUser?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            email.Length > 254 ||
            password.Length > 256)
        {
            return null;
        }

        var normalizedEmail = email.Trim().ToUpperInvariant();
        var user = await dbContext.AdminUsers
            .Include(item => item.Permissions)
            .SingleOrDefaultAsync(
                item => item.NormalizedEmail == normalizedEmail,
                cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (user is null || !user.IsActive || user.IsLockedOut(now))
        {
            return null;
        }

        var verification = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);
        if (verification == PasswordVerificationResult.Failed)
        {
            user.RegisterFailedLogin(now);
            await dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.SetPasswordHash(passwordHasher.HashPassword(user, password));
        }

        user.RegisterSuccessfulLogin(now);
        await dbContext.SaveChangesAsync(cancellationToken);
        var effectivePermissions = await permissionService.GetEffectivePermissionsAsync(user.Id, cancellationToken);
        return new AdminAuthenticatedUser(
            user.Id,
            user.Email,
            user.DisplayName,
            effectivePermissions.OrderBy(item => item).ToArray(),
            user.SecurityStamp,user.IsTwoFactorEnabled,user.AuthorizationVersion);
    }

    public async Task<AdminAuthenticatedUser?> ValidateSessionAsync(
        Guid userId,
        Guid sessionId,
        string securityStamp,
        TimeSpan idleTimeout,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty ||
            sessionId == Guid.Empty ||
            string.IsNullOrWhiteSpace(securityStamp) ||
            securityStamp.Length > 32 ||
            idleTimeout <= TimeSpan.Zero)
        {
            return null;
        }

        var session = await dbContext.AdminSessions
            .SingleOrDefaultAsync(
                item => item.Id == sessionId &&
                    item.AdminUserId == userId,
                cancellationToken);
        if (session is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        if (!session.IsValid(now, idleTimeout))
        {
            return null;
        }

        var user = await dbContext.AdminUsers
            .Include(item => item.Permissions)
            .SingleOrDefaultAsync(
                item => item.Id == userId,
                cancellationToken);
        if (user is null ||
            !user.IsActive ||
            session.AuthorizationVersionAtCreation != user.AuthorizationVersion ||
            !string.Equals(
                user.SecurityStamp,
                securityStamp,
                StringComparison.Ordinal))
        {
            return null;
        }

        session.Touch(now);
        await dbContext.SaveChangesAsync(cancellationToken);

        var effectivePermissions = await permissionService.GetEffectivePermissionsAsync(user.Id, cancellationToken);
        return new AdminAuthenticatedUser(
            user.Id,
            user.Email,
            user.DisplayName,
            effectivePermissions.OrderBy(item => item).ToArray(),
            user.SecurityStamp,user.IsTwoFactorEnabled,user.AuthorizationVersion);
    }

    public async Task CreateSessionAsync(
        Guid userId,
        Guid sessionId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || sessionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Admin user and session identifiers are required.");
        }

        var now = timeProvider.GetUtcNow();
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            expiresAt,
            now);

        var user=await dbContext.AdminUsers.SingleOrDefaultAsync(
                item => item.Id == userId && item.IsActive,
                cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException(
                "The authenticated admin user is no longer active.");
        }

        var session=new AdminSession(sessionId,userId,now,expiresAt);
        session.SetContext(null,null,null,null,null,null,user.SecurityStamp,user.AuthorizationVersion);
        dbContext.AdminSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RevokeSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || sessionId == Guid.Empty)
        {
            return false;
        }

        var session = await dbContext.AdminSessions.SingleOrDefaultAsync(
            item => item.Id == sessionId && item.AdminUserId == userId,
            cancellationToken);
        if (session is null)
        {
            return false;
        }

        session.Revoke(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
