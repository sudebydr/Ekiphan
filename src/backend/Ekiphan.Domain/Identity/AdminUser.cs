using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Identity;

public sealed class AdminUser : Entity
{
    private const int MaximumFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private readonly List<AdminUserGrant> _permissions = [];

    private AdminUser()
    {
    }

    public AdminUser(Guid id, string email, string displayName)
        : base(id)
    {
        UpdateProfile(email, displayName);
        SecurityStamp = Guid.NewGuid().ToString("N");
    }

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string SecurityStamp { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    public int AccessFailedCount { get; private set; }

    public DateTimeOffset? LockoutEnd { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }
    public DateTimeOffset? LastFailedLoginAt { get; private set; }
    public DateTimeOffset? PasswordChangedAt { get; private set; }
    public bool IsTwoFactorEnabled { get; private set; }
    public TwoFactorStatus TwoFactorStatus { get; private set; }
    public string? TwoFactorSecretEncrypted { get; private set; }
    public DateTimeOffset? TwoFactorEnabledAt { get; private set; }
    public DateTimeOffset? TwoFactorLastVerifiedAt { get; private set; }
    public int TwoFactorFailedAttemptCount { get; private set; }
    public DateTimeOffset? TwoFactorLockedUntil { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public long AuthorizationVersion { get; private set; } = 1;
    public DateTimeOffset? LastPermissionChangedAt { get; private set; }

    public IReadOnlyCollection<AdminUserGrant> Permissions => _permissions;

    public bool IsLockedOut(DateTimeOffset now) =>
        LockoutEnd.HasValue && LockoutEnd > now;

    public void UpdateProfile(string email, string displayName)
    {
        var updatedEmail =
            Required(email, 254, nameof(email)).ToLowerInvariant();
        var updatedDisplayName =
            Required(displayName, 150, nameof(displayName));
        if (SecurityStamp.Length > 0 &&
            (!string.Equals(Email, updatedEmail, StringComparison.Ordinal) ||
             !string.Equals(
                 DisplayName,
                 updatedDisplayName,
                 StringComparison.Ordinal)))
        {
            SecurityStamp = Guid.NewGuid().ToString("N");
        }

        Email = updatedEmail;
        NormalizedEmail = Email.ToUpperInvariant();
        DisplayName = updatedDisplayName;
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = Required(passwordHash, 1000, nameof(passwordHash));
        SecurityStamp = Guid.NewGuid().ToString("N");
        AccessFailedCount = 0;
        LockoutEnd = null;
        PasswordChangedAt = DateTimeOffset.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        if (IsActive != isActive)
        {
            IsActive = isActive;
            SecurityStamp = Guid.NewGuid().ToString("N");
        }
    }

    public void SetPermissions(IReadOnlyCollection<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        var normalized = permissions
            .Select(item => item.Trim().ToLowerInvariant())
            .ToArray();
        if (normalized.Length == 0 ||
            normalized.Distinct(StringComparer.Ordinal).Count() !=
                normalized.Length)
        {
            throw new ArgumentException(
                "At least one unique permission is required.",
                nameof(permissions));
        }

        _permissions.Clear();
        _permissions.AddRange(
            normalized.Select(item => new AdminUserGrant(Id, item)));
        SecurityStamp = Guid.NewGuid().ToString("N");
        AuthorizationVersion++;
        LastPermissionChangedAt = DateTimeOffset.UtcNow;
    }

    public void PermissionsChanged(DateTimeOffset at)
    { AuthorizationVersion++; LastPermissionChangedAt=at; SecurityStamp=Guid.NewGuid().ToString("N"); }

    public void RegisterFailedLogin(DateTimeOffset now)
    {
        LastFailedLoginAt = now;
        AccessFailedCount++;
        if (AccessFailedCount >= MaximumFailedAttempts)
        {
            LockoutEnd = now.Add(LockoutDuration);
            AccessFailedCount = 0;
        }
    }

    public void RegisterSuccessfulLogin(DateTimeOffset now)
    {
        AccessFailedCount = 0;
        LockoutEnd = null;
        LastLoginAt = now;
    }

    public void BeginTwoFactorSetup(string protectedSecret)
    {
        TwoFactorSecretEncrypted=Required(protectedSecret,4000,nameof(protectedSecret));
        IsTwoFactorEnabled=false;TwoFactorStatus=TwoFactorStatus.SetupPending;
        TwoFactorFailedAttemptCount=0;TwoFactorLockedUntil=null;
    }

    public void EnableTwoFactor(DateTimeOffset now)
    {
        if(TwoFactorStatus!=TwoFactorStatus.SetupPending||string.IsNullOrWhiteSpace(TwoFactorSecretEncrypted))throw new InvalidOperationException("TWO_FACTOR_SETUP_NOT_FOUND");
        IsTwoFactorEnabled=true;TwoFactorStatus=TwoFactorStatus.Enabled;TwoFactorEnabledAt=now;TwoFactorLastVerifiedAt=now;TwoFactorFailedAttemptCount=0;TwoFactorLockedUntil=null;PermissionsChanged(now);
    }

    public void RegisterTwoFactorFailure(DateTimeOffset now,int maximumAttempts,TimeSpan lockout)
    {
        TwoFactorFailedAttemptCount++;
        if(TwoFactorFailedAttemptCount>=maximumAttempts){TwoFactorStatus=TwoFactorStatus.Locked;TwoFactorLockedUntil=now.Add(lockout);TwoFactorFailedAttemptCount=0;}
    }

    public void RegisterTwoFactorSuccess(DateTimeOffset now)
    { TwoFactorFailedAttemptCount=0;TwoFactorLockedUntil=null;TwoFactorStatus=TwoFactorStatus.Enabled;TwoFactorLastVerifiedAt=now; }

    public bool IsTwoFactorLocked(DateTimeOffset now)=>TwoFactorLockedUntil.HasValue&&TwoFactorLockedUntil>now;

    public void DisableTwoFactor(DateTimeOffset now)
    { IsTwoFactorEnabled=false;TwoFactorStatus=TwoFactorStatus.Disabled;TwoFactorSecretEncrypted=null;TwoFactorEnabledAt=null;TwoFactorLastVerifiedAt=null;TwoFactorFailedAttemptCount=0;TwoFactorLockedUntil=null;PermissionsChanged(now); }

    private static string Required(
        string value,
        int maximumLength,
        string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"Value exceeds {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }
}
