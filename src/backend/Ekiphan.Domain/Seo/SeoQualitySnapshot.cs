using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Seo;

public sealed class SeoQualitySnapshot : Entity
{
    private SeoQualitySnapshot() { }

    public SeoQualitySnapshot(
        Guid id,
        SeoEntityType entityType,
        Guid entityId,
        string languageCode,
        int score,
        int criticalCount,
        int errorCount,
        int warningCount,
        string issuesJson,
        long version)
        : base(id)
    {
        EntityType = entityType;
        EntityId = entityId;
        LanguageCode = languageCode ?? "tr";
        Score = score;
        CriticalCount = criticalCount;
        ErrorCount = errorCount;
        WarningCount = warningCount;
        IssuesJson = issuesJson ?? "[]";
        CalculatedAt = DateTimeOffset.UtcNow;
        Version = version;
    }

    public SeoEntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public string LanguageCode { get; private set; } = "tr";
    public int Score { get; private set; }
    public int CriticalCount { get; private set; }
    public int ErrorCount { get; private set; }
    public int WarningCount { get; private set; }
    public string IssuesJson { get; private set; } = "[]";
    public DateTimeOffset CalculatedAt { get; private set; }
    public long Version { get; private set; }

    public void Update(int score, int critical, int error, int warning, string issuesJson)
    {
        Score = score;
        CriticalCount = critical;
        ErrorCount = error;
        WarningCount = warning;
        IssuesJson = issuesJson;
        CalculatedAt = DateTimeOffset.UtcNow;
        Version++;
    }
}
