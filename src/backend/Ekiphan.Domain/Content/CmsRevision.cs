using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Content;

public enum CmsRevisionChangeType
{
    Created = 1,
    Updated = 2,
    Published = 3,
    Unpublished = 4,
    Scheduled = 5,
    Archived = 6,
    Restored = 7,
    Reordered = 8,
    SettingsChanged = 9,
    MediaChanged = 10,
    ProductRelationChanged = 11
}

public sealed class CmsRevision : Entity
{
    private CmsRevision() { }

    public CmsRevision(
        Guid id,
        string entityType,
        Guid entityId,
        int versionNumber,
        CmsRevisionChangeType changeType,
        string snapshotJson,
        string? changedFieldsJson,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        string? reason = null,
        string? correlationId = null,
        int schemaVersion = 1)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("EntityType is required.", nameof(entityType));
        if (entityId == Guid.Empty) throw new ArgumentException("EntityId is required.", nameof(entityId));
        if (string.IsNullOrWhiteSpace(snapshotJson)) throw new ArgumentException("SnapshotJson is required.", nameof(snapshotJson));

        EntityType = entityType.Trim();
        EntityId = entityId;
        VersionNumber = versionNumber;
        ChangeType = changeType;
        SnapshotJson = snapshotJson;
        ChangedFieldsJson = changedFieldsJson;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
        Reason = reason?.Trim();
        CorrelationId = correlationId?.Trim();
        SchemaVersion = schemaVersion;
    }

    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public int VersionNumber { get; private set; }
    public CmsRevisionChangeType ChangeType { get; private set; }
    public string SnapshotJson { get; private set; } = string.Empty;
    public string? ChangedFieldsJson { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public new DateTimeOffset CreatedAt { get; private set; }
    public string? Reason { get; private set; }
    public string? CorrelationId { get; private set; }
    public int SchemaVersion { get; private set; }
}
