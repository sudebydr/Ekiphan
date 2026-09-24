namespace Ekiphan.Domain.Identity;

public sealed class AdminUserGrant
{
    private AdminUserGrant()
    {
    }

    internal AdminUserGrant(Guid adminUserId, string permission)
    {
        AdminUserId = adminUserId;
        Permission = Normalize(permission);
    }

    public Guid AdminUserId { get; private set; }

    public string Permission { get; private set; } = string.Empty;

    private static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToLowerInvariant();
        if (!AdminPermissionCode.All.Contains(normalized))
        {
            throw new ArgumentException(
                "The admin permission is not supported.",
                nameof(value));
        }

        return normalized;
    }
}
