using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Identity;

public sealed class AdminSession : Entity
{
    private static readonly TimeSpan StoragePrecision =
        TimeSpan.FromSeconds(1);

    private AdminSession()
    {
    }

    public AdminSession(
        Guid id,
        Guid adminUserId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
        : base(id)
    {
        if (adminUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Admin user identifier cannot be empty.",
                nameof(adminUserId));
        }

        var normalizedCreatedAt = NormalizeForStorage(createdAt);
        var normalizedExpiresAt = NormalizeForStorage(expiresAt);
        if (normalizedExpiresAt <= normalizedCreatedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAt),
                "Session expiration must be later than its creation time.");
        }

        AdminUserId = adminUserId;
        LastActivityAt = normalizedCreatedAt;
        ExpiresAt = normalizedExpiresAt;
    }

    public Guid AdminUserId { get; private set; }

    public DateTimeOffset LastActivityAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? RevokedByUserId { get; private set; }
    public string? RevokeReason { get; private set; }
    public string? DeviceId { get; private set; }
    public string? DeviceName { get; private set; }
    public string? UserAgent { get; private set; }
    public string? Browser { get; private set; }
    public string? OperatingSystem { get; private set; }
    public string? IpAddress { get; private set; }
    public string? SecurityStampAtCreation { get; private set; }
    public long AuthorizationVersionAtCreation { get; private set; }

    public bool IsValid(DateTimeOffset now, TimeSpan idleTimeout) =>
        idleTimeout > TimeSpan.Zero &&
        RevokedAt is null &&
        ExpiresAt > now &&
        LastActivityAt.Add(idleTimeout) > now;

    public void Touch(DateTimeOffset now)
    {
        var normalizedNow = NormalizeForStorage(now);
        if (RevokedAt.HasValue ||
            now >= ExpiresAt ||
            normalizedNow.Add(StoragePrecision) < LastActivityAt)
        {
            throw new InvalidOperationException(
                "An inactive admin session cannot be refreshed.");
        }

        if (normalizedNow > LastActivityAt)
        {
            LastActivityAt = normalizedNow;
        }
    }

    public void Revoke(DateTimeOffset revokedAt)
    {
        var normalizedRevokedAt = NormalizeForStorage(revokedAt);
        if (normalizedRevokedAt.Add(StoragePrecision) < LastActivityAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(revokedAt),
                "Revocation cannot precede the session activity time.");
        }

        RevokedAt ??= normalizedRevokedAt < LastActivityAt
            ? LastActivityAt
            : normalizedRevokedAt;
    }

    public void SetContext(string? deviceId,string? deviceName,string? userAgent,string? browser,string? operatingSystem,string? ipAddress,string securityStamp,long authorizationVersion)
    { DeviceId=Trim(deviceId,200);DeviceName=Trim(deviceName,200);UserAgent=Trim(userAgent,1000);Browser=Trim(browser,100);OperatingSystem=Trim(operatingSystem,100);IpAddress=Trim(ipAddress,64);SecurityStampAtCreation=Trim(securityStamp,64);AuthorizationVersionAtCreation=authorizationVersion; }

    public void Revoke(DateTimeOffset revokedAt,Guid? actor,string? reason){Revoke(revokedAt);RevokedByUserId=actor;RevokeReason=Trim(reason,500);}

    private static string? Trim(string? value,int max)=>string.IsNullOrWhiteSpace(value)?null:value.Trim()[..Math.Min(value.Trim().Length,max)];

    private static DateTimeOffset NormalizeForStorage(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        return new DateTimeOffset(
            utc.Ticks - (utc.Ticks % TimeSpan.TicksPerSecond),
            TimeSpan.Zero);
    }
}
