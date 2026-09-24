using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Media;

public sealed class ProductMediaImportBatchItem : Entity
{
    private ProductMediaImportBatchItem() { }

    public ProductMediaImportBatchItem(Guid id, Guid batchId, string temporaryFileId, string originalFileName,
        string? extractedSku, string contentHash, int sortOrder, bool isPrimary) : base(id)
    {
        BatchId = batchId; TemporaryFileId = temporaryFileId; OriginalFileName = Path.GetFileName(originalFileName);
        ExtractedSku = extractedSku; ContentHash = contentHash.ToUpperInvariant(); SortOrder = sortOrder; IsPrimary = isPrimary;
    }

    public Guid BatchId { get; private set; }
    public string TemporaryFileId { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string? ExtractedSku { get; private set; }
    public Guid? ProductId { get; private set; }
    public Guid? MediaAssetId { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public ProductMediaImportBatchItemStatus Status { get; private set; } = ProductMediaImportBatchItemStatus.Pending;
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset? ProductVersionAtImport { get; private set; }

    public void Match(Guid productId, int sortOrder, bool isPrimary)
    { ProductId = productId; SortOrder = sortOrder; IsPrimary = isPrimary; Status = ProductMediaImportBatchItemStatus.Matched; }
    public void Imported(Guid mediaAssetId, DateTimeOffset productVersion)
    { MediaAssetId = mediaAssetId; ProductVersionAtImport = productVersion; Status = ProductMediaImportBatchItemStatus.Imported; }
    public void SetFailure(ProductMediaImportBatchItemStatus status, string code, string message)
    { Status = status; ErrorCode = code; ErrorMessage = message.Length <= 1000 ? message : message[..1000]; }
    public void MarkRolledBack() => Status = ProductMediaImportBatchItemStatus.RolledBack;
    public void MarkRollbackSkipped(string reason) { Status = ProductMediaImportBatchItemStatus.RollbackSkipped; ErrorCode = "ROLLBACK_SKIPPED"; ErrorMessage = reason; }
}
