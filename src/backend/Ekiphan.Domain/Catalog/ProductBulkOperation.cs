using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Catalog;

public enum ProductBulkOperationType
{
    Publish = 1,
    Unpublish = 2,
    Archive = 3,
    ChangeBrand = 4,
    AddCategories = 5,
    RemoveCategories = 6,
    ReplaceCategories = 7,
    AddTags = 8,
    RemoveTags = 9,
    UpdateSeo = 10,
    SubmitForReview = 11,
    RestoreToDraft = 12
}

public enum ProductBulkOperationStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    PartiallyCompleted = 4,
    Failed = 5,
    Cancelled = 6
}

public enum ProductBulkOperationItemStatus
{
    Pending = 1,
    Completed = 2,
    Skipped = 3,
    Failed = 4
}

public sealed class ProductBulkOperation : Entity
{
    private readonly List<ProductBulkOperationItem> _items = [];

    private ProductBulkOperation()
    {
    }

    public ProductBulkOperation(
        Guid id,
        ProductBulkOperationType operationType,
        Guid requestedByUserId,
        DateTimeOffset requestedAt,
        int totalItemCount,
        string? requestPayloadJson = null,
        string? correlationId = null)
        : base(id)
    {
        if (requestedByUserId == Guid.Empty) throw new ArgumentException("RequestedByUserId is required.", nameof(requestedByUserId));
        ArgumentOutOfRangeException.ThrowIfNegative(totalItemCount);

        OperationType = operationType;
        RequestedByUserId = requestedByUserId;
        RequestedAt = requestedAt;
        TotalItemCount = totalItemCount;
        RequestPayloadJson = requestPayloadJson;
        CorrelationId = correlationId;
        Status = ProductBulkOperationStatus.Pending;
    }

    public ProductBulkOperationType OperationType { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public ProductBulkOperationStatus Status { get; private set; }
    public int TotalItemCount { get; private set; }
    public int SuccessCount { get; private set; }
    public int FailedCount { get; private set; }
    public int SkippedCount { get; private set; }
    public string? RequestPayloadJson { get; private set; }
    public string? ResultSummaryJson { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? CorrelationId { get; private set; }

    public IReadOnlyCollection<ProductBulkOperationItem> Items => _items;

    public void AddItem(Guid itemId, Guid productId)
    {
        _items.Add(new ProductBulkOperationItem(itemId, Id, productId));
    }

    public void Start(DateTimeOffset startedAt)
    {
        Status = ProductBulkOperationStatus.Processing;
        StartedAt = startedAt;
    }

    public void UpdateProgress(int success, int failed, int skipped)
    {
        SuccessCount = success;
        FailedCount = failed;
        SkippedCount = skipped;
    }

    public void Complete(DateTimeOffset completedAt, string? resultSummaryJson = null)
    {
        CompletedAt = completedAt;
        ResultSummaryJson = resultSummaryJson;
        if (FailedCount == 0 && SkippedCount == 0)
        {
            Status = ProductBulkOperationStatus.Completed;
        }
        else if (SuccessCount > 0)
        {
            Status = ProductBulkOperationStatus.PartiallyCompleted;
        }
        else
        {
            Status = ProductBulkOperationStatus.Failed;
        }
    }

    public void Fail(DateTimeOffset failedAt, string errorMessage)
    {
        CompletedAt = failedAt;
        ErrorMessage = errorMessage;
        Status = ProductBulkOperationStatus.Failed;
    }
}

public sealed class ProductBulkOperationItem : Entity
{
    private ProductBulkOperationItem()
    {
    }

    public ProductBulkOperationItem(
        Guid id,
        Guid bulkOperationId,
        Guid productId)
        : base(id)
    {
        if (bulkOperationId == Guid.Empty) throw new ArgumentException("BulkOperationId is required.", nameof(bulkOperationId));
        if (productId == Guid.Empty) throw new ArgumentException("ProductId is required.", nameof(productId));

        BulkOperationId = bulkOperationId;
        ProductId = productId;
        Status = ProductBulkOperationItemStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid BulkOperationId { get; private set; }
    public Guid ProductId { get; private set; }
    public ProductBulkOperationItemStatus Status { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int? PreviousVersionNumber { get; private set; }
    public int? NewVersionNumber { get; private set; }
    public new DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public void MarkCompleted(int? previousVersion, int? newVersion, DateTimeOffset completedAt)
    {
        Status = ProductBulkOperationItemStatus.Completed;
        PreviousVersionNumber = previousVersion;
        NewVersionNumber = newVersion;
        CompletedAt = completedAt;
    }

    public void MarkSkipped(string? reason, DateTimeOffset completedAt)
    {
        Status = ProductBulkOperationItemStatus.Skipped;
        ErrorMessage = reason;
        CompletedAt = completedAt;
    }

    public void MarkFailed(string errorCode, string errorMessage, DateTimeOffset completedAt)
    {
        Status = ProductBulkOperationItemStatus.Failed;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        CompletedAt = completedAt;
    }
}
