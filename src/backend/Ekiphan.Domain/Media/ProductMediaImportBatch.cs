using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Media;

public sealed class ProductMediaImportBatch : Entity
{
    private readonly List<ProductMediaImportBatchItem> _items = [];

    private ProductMediaImportBatch() { }

    public ProductMediaImportBatch(Guid id, string fileName, string originalFileHash, Guid createdByUserId)
        : base(id)
    {
        FileName = Path.GetFileName(fileName);
        OriginalFileHash = originalFileHash.ToUpperInvariant();
        CreatedByUserId = createdByUserId;
    }

    public string FileName { get; private set; } = string.Empty;
    public string OriginalFileHash { get; private set; } = string.Empty;
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int TotalFiles { get; private set; }
    public int MatchedFileCount { get; private set; }
    public int ImportedFileCount { get; private set; }
    public int SkippedFileCount { get; private set; }
    public int ErrorFileCount { get; private set; }
    public ProductMediaImportBatchStatus Status { get; private set; } = ProductMediaImportBatchStatus.Uploaded;
    public DateTimeOffset? RolledBackAt { get; private set; }
    public Guid? RolledBackByUserId { get; private set; }
    public string? FailureReason { get; private set; }
    public IReadOnlyCollection<ProductMediaImportBatchItem> Items => _items;

    public void SetValidationSummary(int total, int matched, int skipped, int errors)
    {
        TotalFiles = total; MatchedFileCount = matched; SkippedFileCount = skipped; ErrorFileCount = errors;
        Status = ProductMediaImportBatchStatus.Validated;
    }

    public void Start(DateTimeOffset now) { StartedAt = now; Status = ProductMediaImportBatchStatus.Processing; }

    public void Complete(DateTimeOffset now, int imported, int skipped, int errors)
    {
        CompletedAt = now; ImportedFileCount = imported; SkippedFileCount = skipped; ErrorFileCount = errors;
        Status = errors == 0 ? ProductMediaImportBatchStatus.Completed : ProductMediaImportBatchStatus.PartiallyCompleted;
    }

    public void Fail(DateTimeOffset now, string reason, bool compensationRequired = false)
    {
        CompletedAt = now; FailureReason = reason.Length <= 1000 ? reason : reason[..1000];
        Status = compensationRequired ? ProductMediaImportBatchStatus.CompensationRequired : ProductMediaImportBatchStatus.Failed;
    }

    public void MarkRolledBack(DateTimeOffset now, Guid actor)
    {
        if (Status == ProductMediaImportBatchStatus.RolledBack) throw new InvalidOperationException("Batch has already been rolled back.");
        RolledBackAt = now; RolledBackByUserId = actor; Status = ProductMediaImportBatchStatus.RolledBack;
    }

    public void AddItem(ProductMediaImportBatchItem item) => _items.Add(item);
}
