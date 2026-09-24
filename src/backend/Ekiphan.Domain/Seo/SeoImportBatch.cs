using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Seo;

public enum SeoImportStatus
{
    Pending = 1,
    Validating = 2,
    Executing = 3,
    Completed = 4,
    Failed = 5,
    RolledBack = 6
}

public sealed class SeoImportBatch : Entity
{
    private SeoImportBatch() { }

    public SeoImportBatch(
        Guid id,
        Guid createdByUserId,
        string fileName,
        int totalRows,
        string? correlationId = null)
        : base(id)
    {
        CreatedByUserId = createdByUserId;
        FileName = fileName ?? string.Empty;
        TotalRows = totalRows;
        Status = SeoImportStatus.Pending;
        CorrelationId = correlationId;
    }

    public Guid CreatedByUserId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public int TotalRows { get; private set; }
    public int SuccessRows { get; private set; }
    public int ErrorRows { get; private set; }
    public SeoImportStatus Status { get; private set; } = SeoImportStatus.Pending;
    public string? ErrorSummary { get; private set; }
    public string? CorrelationId { get; private set; }

    public ICollection<SeoImportBatchItem> Items { get; private set; } = new List<SeoImportBatchItem>();

    public void Complete(int successCount, int errorCount)
    {
        SuccessRows = successCount;
        ErrorRows = errorCount;
        Status = errorCount == 0 ? SeoImportStatus.Completed : SeoImportStatus.Failed;
    }

    public void MarkRolledBack()
    {
        Status = SeoImportStatus.RolledBack;
    }
}

public sealed class SeoImportBatchItem : Entity
{
    private SeoImportBatchItem() { }

    public SeoImportBatchItem(
        Guid id,
        Guid batchId,
        int rowNumber,
        SeoEntityType entityType,
        Guid entityId,
        string languageCode,
        string rawDataJson,
        bool isValid,
        string? errorMessage = null)
        : base(id)
    {
        BatchId = batchId;
        RowNumber = rowNumber;
        EntityType = entityType;
        EntityId = entityId;
        LanguageCode = languageCode ?? "tr";
        RawDataJson = rawDataJson ?? "{}";
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public Guid BatchId { get; private set; }
    public SeoImportBatch? Batch { get; private set; }
    public int RowNumber { get; private set; }
    public SeoEntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public string LanguageCode { get; private set; } = "tr";
    public string RawDataJson { get; private set; } = "{}";
    public bool IsValid { get; private set; }
    public string? ErrorMessage { get; private set; }
}
