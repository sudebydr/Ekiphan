using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.DataImport;

public sealed class ImportJob : Entity
{
    private readonly List<ImportRow> _rows = [];

    private ImportJob()
    {
    }

    public ImportJob(
        Guid id,
        ImportSourceType sourceType,
        string originalFileName,
        string sourceSha256Checksum,
        bool isDryRun,
        Guid? createdByUserId = null)
        : base(id)
    {
        if (!Enum.IsDefined(sourceType))
        {
            throw new ArgumentOutOfRangeException(nameof(sourceType));
        }

        if (sourceType == ImportSourceType.IdeaSoftApi &&
            !string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException(
                "API imports cannot define an original file name.",
                nameof(originalFileName));
        }

        if (sourceType != ImportSourceType.IdeaSoftApi)
        {
            var safeFileName = Path.GetFileName(originalFileName);

            if (!string.Equals(originalFileName, safeFileName, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Original file name cannot contain a path.",
                    nameof(originalFileName));
            }

            OriginalFileName = ImportGuard.Required(
                safeFileName,
                260,
                nameof(originalFileName));
        }

        SourceType = sourceType;
        SourceSha256Checksum = ImportGuard.Sha256(
            sourceSha256Checksum,
            nameof(sourceSha256Checksum));
        IsDryRun = isDryRun;
        CreatedByUserId = createdByUserId;
    }

    public ImportSourceType SourceType { get; private set; }

    public ImportJobStatus Status { get; private set; } = ImportJobStatus.Uploaded;

    public string? OriginalFileName { get; private set; }

    public string SourceSha256Checksum { get; private set; } = string.Empty;

    public bool IsDryRun { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? ValidationStartedAt { get; private set; }

    public DateTimeOffset? ValidationCompletedAt { get; private set; }

    public DateTimeOffset? PublishingStartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public string? FailureReason { get; private set; }

    public int TotalRowCount { get; private set; }

    public int ValidRowCount { get; private set; }

    public int InvalidRowCount { get; private set; }

    public int WarningCount { get; private set; }

    public int PublishedRowCount { get; private set; }

    public IReadOnlyCollection<ImportRow> Rows => _rows;

    public void StartValidation(DateTimeOffset startedAt)
    {
        if (Status is not ImportJobStatus.Uploaded and not ImportJobStatus.Failed)
        {
            throw new InvalidOperationException(
                $"Import job cannot start validation from '{Status}'.");
        }

        Status = ImportJobStatus.Validating;
        ValidationStartedAt = startedAt;
        ValidationCompletedAt = null;
        FailureReason = null;
    }

    public ImportRow AddRow(
        Guid rowId,
        string sheetName,
        int rowNumber,
        string rawPayload,
        string? sku = null)
    {
        if (Status != ImportJobStatus.Validating)
        {
            throw new InvalidOperationException(
                "Rows can only be added while the import job is validating.");
        }

        if (_rows.Any(
                row =>
                    row.SheetName.Equals(sheetName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    row.RowNumber == rowNumber))
        {
            throw new InvalidOperationException(
                "The sheet and row number already exists in this import job.");
        }

        var row = new ImportRow(rowId, Id, sheetName, rowNumber, rawPayload, sku);
        _rows.Add(row);
        TotalRowCount = _rows.Count;
        return row;
    }

    public void CompleteValidation(DateTimeOffset completedAt)
    {
        if (Status != ImportJobStatus.Validating)
        {
            throw new InvalidOperationException(
                "Only a validating import job can complete validation.");
        }

        if (_rows.Count == 0)
        {
            throw new InvalidOperationException(
                "An import job must contain at least one data row.");
        }

        if (_rows.Any(row => row.Status == ImportRowStatus.Pending))
        {
            throw new InvalidOperationException(
                "All import rows must be validated before validation can complete.");
        }

        ValidRowCount = _rows.Count(row => row.Status == ImportRowStatus.Valid);
        InvalidRowCount = _rows.Count(row => row.Status == ImportRowStatus.Invalid);
        WarningCount = _rows.Sum(
            row => row.Issues.Count(issue => issue.Severity == ImportIssueSeverity.Warning));
        ValidationCompletedAt = completedAt;
        if (InvalidRowCount > 0 || ValidRowCount == 0)
        {
            Status = ImportJobStatus.ValidationFailed;
        }
        else if (IsDryRun)
        {
            Status = ImportJobStatus.Completed;
            CompletedAt = completedAt;
        }
        else
        {
            Status = ImportJobStatus.ReadyToPublish;
        }
    }

    public void StartPublishing(DateTimeOffset startedAt)
    {
        if (Status != ImportJobStatus.ReadyToPublish)
        {
            throw new InvalidOperationException(
                "Only a validated import job can start publishing.");
        }

        if (IsDryRun)
        {
            throw new InvalidOperationException(
                "A dry-run import job cannot publish data.");
        }

        Status = ImportJobStatus.Publishing;
        PublishingStartedAt = startedAt;
    }

    public void CompletePublishing(DateTimeOffset completedAt)
    {
        if (Status != ImportJobStatus.Publishing)
        {
            throw new InvalidOperationException(
                "Only a publishing import job can be completed.");
        }

        if (_rows.Any(row => row.Status == ImportRowStatus.Valid))
        {
            throw new InvalidOperationException(
                "All valid rows must be published or skipped before completion.");
        }

        PublishedRowCount = _rows.Count(row => row.Status == ImportRowStatus.Published);
        Status = ImportJobStatus.Completed;
        CompletedAt = completedAt;
    }

    public void Fail(string reason, DateTimeOffset failedAt)
    {
        if (Status is ImportJobStatus.Completed or ImportJobStatus.Cancelled)
        {
            throw new InvalidOperationException(
                $"Import job cannot fail from terminal status '{Status}'.");
        }

        Status = ImportJobStatus.Failed;
        FailureReason = ImportGuard.Required(reason, 2000, nameof(reason));
        CompletedAt = failedAt;
    }

    public void Cancel(DateTimeOffset cancelledAt)
    {
        if (Status is ImportJobStatus.Completed or ImportJobStatus.Cancelled)
        {
            throw new InvalidOperationException(
                $"Import job cannot be cancelled from '{Status}'.");
        }

        Status = ImportJobStatus.Cancelled;
        CompletedAt = cancelledAt;
    }
}
