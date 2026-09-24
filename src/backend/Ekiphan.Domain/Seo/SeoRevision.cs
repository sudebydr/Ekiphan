using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Seo;

public sealed class SeoRevision : Entity
{
    private SeoRevision() { }

    public SeoRevision(
        Guid id,
        SeoEntityType entityType,
        Guid entityId,
        string languageCode,
        int versionNumber,
        string snapshotJson,
        string? changedFieldsJson,
        SeoChangeSource changeSource,
        Guid createdByUserId,
        string? reason = null,
        Guid? importBatchId = null,
        string? correlationId = null)
        : base(id)
    {
        EntityType = entityType;
        EntityId = entityId;
        LanguageCode = languageCode ?? "tr";
        VersionNumber = versionNumber;
        SnapshotJson = snapshotJson ?? throw new ArgumentNullException(nameof(snapshotJson));
        ChangedFieldsJson = changedFieldsJson;
        ChangeSource = changeSource;
        CreatedByUserId = createdByUserId;
        Reason = reason;
        ImportBatchId = importBatchId;
        CorrelationId = correlationId;
    }

    public SeoEntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public string LanguageCode { get; private set; } = "tr";
    public int VersionNumber { get; private set; }
    public string SnapshotJson { get; private set; } = string.Empty;
    public string? ChangedFieldsJson { get; private set; }
    public SeoChangeSource ChangeSource { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string? Reason { get; private set; }
    public Guid? ImportBatchId { get; private set; }
    public string? CorrelationId { get; private set; }
}
