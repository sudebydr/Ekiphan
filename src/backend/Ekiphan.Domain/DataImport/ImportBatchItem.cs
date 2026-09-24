using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.DataImport;

public sealed class ImportBatchItem : Entity
{
    private ImportBatchItem() { }

    public ImportBatchItem(Guid id, Guid importBatchId, int rowNumber, string? sku,
        ImportBatchItemStatus status, string? errorCode = null, string? errorMessage = null,
        string? errorField = null) : base(id)
    {
        if (importBatchId == Guid.Empty) throw new ArgumentException("Batch is required.", nameof(importBatchId));
        ArgumentOutOfRangeException.ThrowIfLessThan(rowNumber, 2);
        ImportBatchId = importBatchId;
        RowNumber = rowNumber;
        Sku = string.IsNullOrWhiteSpace(sku) ? null : sku.Trim().ToUpperInvariant();
        Status = status;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ErrorField = errorField;
    }

    public Guid ImportBatchId { get; private set; }
    public int RowNumber { get; private set; }
    public Guid? ProductId { get; private set; }
    public string? Sku { get; private set; }
    public ImportBatchItemStatus Status { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ErrorField { get; private set; }
    public DateTimeOffset? ImportedProductVersion { get; private set; }

    public void MarkImported(Guid productId, DateTimeOffset version)
    {
        if (Status != ImportBatchItemStatus.Valid) throw new InvalidOperationException("Only a valid row can be imported.");
        ProductId = productId;
        ImportedProductVersion = version;
        Status = ImportBatchItemStatus.Imported;
    }

    public void MarkRollback(bool skipped)
    {
        if (Status != ImportBatchItemStatus.Imported) throw new InvalidOperationException("Only imported rows can be rolled back.");
        Status = skipped ? ImportBatchItemStatus.RollbackSkipped : ImportBatchItemStatus.RolledBack;
    }
}
