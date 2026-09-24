using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.DataImport;

public sealed class ImportBatch : Entity
{
    private readonly List<ImportBatchItem> _items = [];

    private ImportBatch() { }

    public ImportBatch(
        Guid id,
        string fileName,
        string originalFileHash,
        Guid createdByUserId) : base(id)
    {
        FileName = ImportGuard.Required(Path.GetFileName(fileName), 260, nameof(fileName));
        OriginalFileHash = ImportGuard.Sha256(originalFileHash, nameof(originalFileHash));
        if (createdByUserId == Guid.Empty) throw new ArgumentException("Creator is required.", nameof(createdByUserId));
        CreatedByUserId = createdByUserId;
    }

    public string FileName { get; private set; } = string.Empty;
    public string OriginalFileHash { get; private set; } = string.Empty;
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int TotalRows { get; private set; }
    public int SuccessCount { get; private set; }
    public int ErrorCount { get; private set; }
    public ImportBatchStatus Status { get; private set; } = ImportBatchStatus.Uploaded;
    public DateTimeOffset? RolledBackAt { get; private set; }
    public Guid? RolledBackByUserId { get; private set; }
    public string? FailureReason { get; private set; }
    public string? RollbackSummary { get; private set; }
    public IReadOnlyCollection<ImportBatchItem> Items => _items;

    public void StartValidation(DateTimeOffset now)
    {
        if (Status != ImportBatchStatus.Uploaded) throw new InvalidOperationException("Only an uploaded batch can be validated.");
        Status = ImportBatchStatus.Validating;
        StartedAt = now;
    }

    public void SetValidation(IEnumerable<ImportBatchItem> items, DateTimeOffset now)
    {
        if (Status != ImportBatchStatus.Validating) throw new InvalidOperationException("Batch is not validating.");
        _items.Clear();
        _items.AddRange(items);
        TotalRows = _items.Count;
        ErrorCount = _items.Count(x => x.Status == ImportBatchItemStatus.Invalid);
        Status = ImportBatchStatus.Validated;
        CompletedAt = now;
    }

    public void StartProcessing(DateTimeOffset now)
    {
        if (Status != ImportBatchStatus.Validated) throw new InvalidOperationException("Only a validated batch can be executed.");
        Status = ImportBatchStatus.Processing;
        StartedAt = now;
        CompletedAt = null;
    }

    public void CompleteProcessing(bool partial, DateTimeOffset now)
    {
        if (Status != ImportBatchStatus.Processing) throw new InvalidOperationException("Batch is not processing.");
        SuccessCount = _items.Count(x => x.Status == ImportBatchItemStatus.Imported);
        ErrorCount = _items.Count(x => x.Status == ImportBatchItemStatus.Invalid);
        Status = partial ? ImportBatchStatus.PartiallyCompleted : ImportBatchStatus.Completed;
        CompletedAt = now;
    }

    public void Fail(string reason, DateTimeOffset now)
    {
        if (Status == ImportBatchStatus.RolledBack) throw new InvalidOperationException("A rolled back batch cannot fail.");
        Status = ImportBatchStatus.Failed;
        FailureReason = ImportGuard.Required(reason, 2000, nameof(reason));
        CompletedAt = now;
    }

    public void CompleteRollback(Guid actorId, int rolledBack, int skipped, DateTimeOffset now)
    {
        if (Status is not (ImportBatchStatus.Completed or ImportBatchStatus.PartiallyCompleted))
            throw new InvalidOperationException("Only a completed batch can be rolled back.");
        if (RolledBackAt.HasValue) throw new InvalidOperationException("Batch was already rolled back.");
        Status = ImportBatchStatus.RolledBack;
        RolledBackAt = now;
        RolledBackByUserId = actorId;
        RollbackSummary = $"RolledBack={rolledBack};Skipped={skipped}";
    }
}
