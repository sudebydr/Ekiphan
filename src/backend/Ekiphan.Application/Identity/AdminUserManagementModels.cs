namespace Ekiphan.Application.Identity;

public sealed record AdminUserSummary(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    int AccessFailedCount,
    DateTimeOffset? LockoutEnd,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<string> Permissions,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AdminUserListQuery(
    int Page = 1,
    int PageSize = 50,
    string? Search = null);

public sealed record AdminUserPage(
    IReadOnlyList<AdminUserSummary> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record CreateAdminUserCommand(
    string Email,
    string DisplayName,
    string Password,
    bool IsActive,
    IReadOnlyList<string> Permissions);

public sealed record UpdateAdminUserCommand(
    string Email,
    string DisplayName,
    bool IsActive,
    IReadOnlyList<string> Permissions);

public interface IAdminUserManagementService
{
    Task<AdminUserPage> GetUsersAsync(
        AdminUserListQuery query,
        CancellationToken cancellationToken = default);

    Task<AdminUserSummary> CreateAsync(
        CreateAdminUserCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminUserSummary?> UpdateAsync(
        Guid userId,
        UpdateAdminUserCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> ResetPasswordAsync(
        Guid userId,
        string password,
        CancellationToken cancellationToken = default);
}

public sealed class AdminUserConflictException(string message)
    : Exception(message);
