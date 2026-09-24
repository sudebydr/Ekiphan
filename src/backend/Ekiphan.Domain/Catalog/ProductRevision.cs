using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Catalog;

public enum ProductRevisionChangeType
{
    Created = 1,
    Updated = 2,
    SubmittedForReview = 3,
    Published = 4,
    Unpublished = 5,
    Archived = 6,
    Restored = 7,
    Duplicated = 8,
    Imported = 9,
    BulkUpdated = 10,
    CategoryChanged = 11,
    BrandChanged = 12,
    TagsChanged = 13,
    SeoUpdated = 14,
    MediaChanged = 15,
    VariantChanged = 16
}

public enum ProductRevisionSource
{
    Manual = 1,
    Import = 2,
    BulkOperation = 3,
    System = 4,
    Rollback = 5
}

public sealed class ProductRevision : Entity
{
    private ProductRevision()
    {
    }

    public ProductRevision(
        Guid id,
        Guid productId,
        int versionNumber,
        ProductRevisionChangeType changeType,
        string snapshotJson,
        string? changedFieldsJson,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        string? reason = null,
        string? correlationId = null,
        ProductRevisionSource source = ProductRevisionSource.Manual,
        Guid? importBatchId = null,
        Guid? bulkOperationId = null)
        : base(id)
    {
        if (productId == Guid.Empty) throw new ArgumentException("ProductId is required.", nameof(productId));
        if (versionNumber <= 0) throw new ArgumentOutOfRangeException(nameof(versionNumber), "Version number must be positive.");
        if (string.IsNullOrWhiteSpace(snapshotJson)) throw new ArgumentException("Snapshot JSON is required.", nameof(snapshotJson));
        if (createdByUserId == Guid.Empty) throw new ArgumentException("CreatedByUserId is required.", nameof(createdByUserId));

        ProductId = productId;
        VersionNumber = versionNumber;
        ChangeType = changeType;
        SnapshotJson = snapshotJson;
        ChangedFieldsJson = changedFieldsJson;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
        Reason = reason;
        CorrelationId = correlationId;
        Source = source;
        ImportBatchId = importBatchId;
        BulkOperationId = bulkOperationId;
    }

    public Guid ProductId { get; private set; }
    public int VersionNumber { get; private set; }
    public ProductRevisionChangeType ChangeType { get; private set; }
    public string SnapshotJson { get; private set; } = string.Empty;
    public string? ChangedFieldsJson { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public new DateTimeOffset CreatedAt { get; private set; }
    public string? Reason { get; private set; }
    public string? CorrelationId { get; private set; }
    public ProductRevisionSource Source { get; private set; }
    public Guid? ImportBatchId { get; private set; }
    public Guid? BulkOperationId { get; private set; }
}
