using System.Data;
using System.Net.Mail;
using Ekiphan.Application.Identity;
using Ekiphan.Domain.Identity;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Identity;

internal sealed class AdminUserManagementService(
    EkiphanDbContext dbContext,
    IPasswordHasher<AdminUser> passwordHasher,
    IUserPermissionService permissionService,
    TimeProvider clock)
    : IAdminUserManagementService
{
    public async Task<AdminUserPage> GetUsersAsync(
        AdminUserListQuery query,
        CancellationToken cancellationToken = default)
    {
        Validate(query);
        var users = dbContext.AdminUsers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            var normalizedSearch = search.ToUpperInvariant();
            users = users.Where(item =>
                item.NormalizedEmail.Contains(normalizedSearch) ||
                item.DisplayName.Contains(search));
        }

        var totalCount = await users.CountAsync(cancellationToken);
        var rows = await users
            .OrderBy(item => item.Email)
            .ThenBy(item => item.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(item => new AdminUserRow(
                item.Id,
                item.Email,
                item.DisplayName,
                item.IsActive,
                item.AccessFailedCount,
                item.LockoutEnd,
                item.LastLoginAt,
                item.CreatedAt,
                item.UpdatedAt))
            .ToListAsync(cancellationToken);

        var userIds = rows.Select(item => item.Id).ToArray();
        var permissionRows = userIds.Length == 0
            ? []
            : await dbContext.Set<AdminUserGrant>()
                .AsNoTracking()
                .Where(item => userIds.Contains(item.AdminUserId))
                .OrderBy(item => item.AdminUserId)
                .ThenBy(item => item.Permission)
                .Select(item => new AdminUserPermissionRow(
                    item.AdminUserId,
                    item.Permission))
                .ToListAsync(cancellationToken);
        var permissionsByUser = permissionRows
            .GroupBy(item => item.AdminUserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group
                    .Select(item => item.Permission)
                    .ToArray());
        var items = rows
            .Select(item => item.ToSummary(
                permissionsByUser.GetValueOrDefault(item.Id, [])))
            .ToArray();

        return new AdminUserPage(
            items,
            query.Page,
            query.PageSize,
            totalCount);
    }

    public async Task<AdminUserSummary> CreateAsync(
        CreateAdminUserCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command.Email, command.DisplayName, command.Permissions);
        AdminPasswordPolicy.EnsureStrong(command.Password);
        var user = new AdminUser(
            Guid.NewGuid(),
            command.Email,
            command.DisplayName);
        user.SetPasswordHash(
            passwordHasher.HashPassword(user, command.Password));
        user.SetPermissions(command.Permissions);
        user.SetActive(command.IsActive);
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
        if (await dbContext.AdminUsers.AnyAsync(
                item => item.NormalizedEmail == user.NormalizedEmail,
                cancellationToken))
        {
            throw new AdminUserConflictException(
                "The admin email is already in use.");
        }

        dbContext.AdminUsers.Add(user);
        await SaveAsync(cancellationToken);
        var result = ToSummary(user);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<AdminUserSummary?> UpdateAsync(
        Guid userId,
        UpdateAdminUserCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command.Email, command.DisplayName, command.Permissions);
        var user = await dbContext.AdminUsers
            .Include(item => item.Permissions)
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var keepsUserManagement =
            command.Permissions.Contains(
                AdminPermissionCode.UsersManage,
                StringComparer.OrdinalIgnoreCase);
        if ((!command.IsActive || !keepsUserManagement) &&
            user.IsActive &&
            user.Permissions.Any(
                item => item.Permission == AdminPermissionCode.UsersManage) &&
            !await HasAnotherActiveUserManagerAsync(
                userId,
                cancellationToken))
        {
            throw new AdminUserConflictException(
                "The final active user manager cannot be disabled or lose users.manage.");
        }

        if (!command.IsActive && user.IsActive &&
            await IsSuperAdminAsync(userId, cancellationToken) &&
            !await HasAnotherActiveSuperAdminAsync(userId, cancellationToken))
        {
            throw new AdminUserConflictException(
                "LAST_SUPER_ADMIN_PROTECTED");
        }

        user.UpdateProfile(command.Email, command.DisplayName);
        user.SetPermissions(command.Permissions);
        user.SetActive(command.IsActive);
        if (!command.IsActive)
        {
            RevokeSessions(userId);
            permissionService.Invalidate(userId);
        }
        await SaveAsync(cancellationToken);
        return ToSummary(user);
    }

    public async Task<bool> ResetPasswordAsync(
        Guid userId,
        string password,
        CancellationToken cancellationToken = default)
    {
        AdminPasswordPolicy.EnsureStrong(password);
        var user = await dbContext.AdminUsers.SingleOrDefaultAsync(
            item => item.Id == userId,
            cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.SetPasswordHash(passwordHasher.HashPassword(user, password));
        RevokeSessions(userId);
        permissionService.Invalidate(userId);
        await SaveAsync(cancellationToken);
        return true;
    }

    private static AdminUserSummary ToSummary(AdminUser user) =>
        new(
            user.Id,
            user.Email,
            user.DisplayName,
            user.IsActive,
            user.AccessFailedCount,
            user.LockoutEnd,
            user.LastLoginAt,
            user.Permissions
                .Select(item => item.Permission)
                .OrderBy(item => item)
                .ToArray(),
            user.CreatedAt,
            user.UpdatedAt);

    private Task<bool> HasAnotherActiveUserManagerAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.AdminUsers.AnyAsync(
            item =>
                item.Id != userId &&
                item.IsActive &&
                item.Permissions.Any(
                    grant =>
                        grant.Permission == AdminPermissionCode.UsersManage),
            cancellationToken);

    private Task<bool> IsSuperAdminAsync(Guid userId,CancellationToken cancellationToken) =>
        dbContext.AdminUserRoles.AnyAsync(link => link.AdminUserId == userId &&
            dbContext.AdminRoles.Any(role => role.Id == link.RoleId && role.IsActive && role.NormalizedName == "SUPERADMIN"), cancellationToken);

    private Task<bool> HasAnotherActiveSuperAdminAsync(Guid userId,CancellationToken cancellationToken) =>
        dbContext.AdminUserRoles.AnyAsync(link => link.AdminUserId != userId &&
            dbContext.AdminUsers.Any(user => user.Id == link.AdminUserId && user.IsActive) &&
            dbContext.AdminRoles.Any(role => role.Id == link.RoleId && role.IsActive && role.NormalizedName == "SUPERADMIN"), cancellationToken);

    private void RevokeSessions(Guid userId)
    {
        var now=clock.GetUtcNow();
        foreach(var session in dbContext.AdminSessions.Where(x=>x.AdminUserId==userId&&x.RevokedAt==null))session.Revoke(now);
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new AdminUserConflictException(
                "The admin email is already in use.");
        }
    }

    private static void Validate(
        string email,
        string displayName,
        IReadOnlyList<string> permissions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(permissions);
        if (!MailAddress.TryCreate(email.Trim(), out var address) ||
            !string.Equals(
                address.Address,
                email.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "A valid email address is required.",
                nameof(email));
        }

        if (permissions.Count == 0 ||
            permissions.Any(
                item => !AdminPermissionCode.All.Contains(
                    item.Trim().ToLowerInvariant())) ||
            permissions.Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
                permissions.Count)
        {
            throw new ArgumentException(
                "Permissions must be unique supported values.",
                nameof(permissions));
        }
    }

    private static void Validate(AdminUserListQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(query.Page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(query.PageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(query.PageSize, 100);
        if (query.Search is { Length: > 100 })
        {
            throw new ArgumentException(
                "Search cannot exceed 100 characters.",
                nameof(query));
        }
    }

    private sealed record AdminUserRow(
        Guid Id,
        string Email,
        string DisplayName,
        bool IsActive,
        int AccessFailedCount,
        DateTimeOffset? LockoutEnd,
        DateTimeOffset? LastLoginAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt)
    {
        public AdminUserSummary ToSummary(
            IReadOnlyList<string> permissions) =>
            new(
                Id,
                Email,
                DisplayName,
                IsActive,
                AccessFailedCount,
                LockoutEnd,
                LastLoginAt,
                permissions,
                CreatedAt,
                UpdatedAt);
    }

    private sealed record AdminUserPermissionRow(
        Guid AdminUserId,
        string Permission);
}
