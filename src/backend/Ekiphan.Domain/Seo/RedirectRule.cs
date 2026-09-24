using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Seo;

public sealed class RedirectRule : Entity
{
    private RedirectRule() { }

    public RedirectRule(
        Guid id,
        string sourcePath,
        string normalizedSourcePath,
        string destinationUrl,
        RedirectType redirectType,
        RedirectMatchType matchType,
        RedirectRuleSourceType sourceType,
        bool preserveQueryString,
        int priority,
        Guid createdByUserId,
        SeoEntityType? sourceEntityType = null,
        Guid? sourceEntityId = null)
        : base(id)
    {
        SourcePath = sourcePath ?? throw new ArgumentNullException(nameof(sourcePath));
        NormalizedSourcePath = normalizedSourcePath ?? throw new ArgumentNullException(nameof(normalizedSourcePath));
        DestinationUrl = destinationUrl ?? throw new ArgumentNullException(nameof(destinationUrl));
        RedirectType = redirectType;
        MatchType = matchType;
        SourceType = sourceType;
        PreserveQueryString = preserveQueryString;
        Priority = priority;
        CreatedByUserId = createdByUserId;
        UpdatedByUserId = createdByUserId;
        IsActive = true;
        SourceEntityType = sourceEntityType;
        SourceEntityId = sourceEntityId;
        RowVersion = Array.Empty<byte>();
    }

    public string SourcePath { get; private set; } = string.Empty;
    public string NormalizedSourcePath { get; private set; } = string.Empty;
    public string DestinationUrl { get; private set; } = string.Empty;
    public RedirectType RedirectType { get; private set; } = RedirectType.Permanent301;
    public RedirectMatchType MatchType { get; private set; } = RedirectMatchType.Exact;
    public RedirectRuleSourceType SourceType { get; private set; } = RedirectRuleSourceType.Manual;
    public SeoEntityType? SourceEntityType { get; private set; }
    public Guid? SourceEntityId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool PreserveQueryString { get; private set; } = true;
    public int Priority { get; private set; } = 100;
    public long HitCount { get; private set; }
    public DateTimeOffset? LastHitAt { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public void Update(
        string sourcePath,
        string normalizedSourcePath,
        string destinationUrl,
        RedirectType redirectType,
        RedirectMatchType matchType,
        bool preserveQueryString,
        int priority,
        Guid actorUserId)
    {
        SourcePath = sourcePath;
        NormalizedSourcePath = normalizedSourcePath;
        DestinationUrl = destinationUrl;
        RedirectType = redirectType;
        MatchType = matchType;
        PreserveQueryString = preserveQueryString;
        Priority = priority;
        UpdatedByUserId = actorUserId;
    }

    public void RecordHit()
    {
        HitCount++;
        LastHitAt = DateTimeOffset.UtcNow;
    }

    public void Archive(Guid actorUserId)
    {
        IsActive = false;
        ArchivedAt = DateTimeOffset.UtcNow;
        UpdatedByUserId = actorUserId;
    }

    public void Activate(Guid actorUserId)
    {
        IsActive = true;
        ArchivedAt = null;
        UpdatedByUserId = actorUserId;
    }
}
