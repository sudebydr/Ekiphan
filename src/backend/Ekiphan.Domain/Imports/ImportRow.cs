using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.DataImport;

public sealed class ImportRow : Entity
{
    private readonly List<ImportIssue> _issues = [];

    private ImportRow()
    {
    }

    internal ImportRow(
        Guid id,
        Guid importJobId,
        string sheetName,
        int rowNumber,
        string rawPayload,
        string? sku)
        : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rowNumber, 1);

        ImportJobId = importJobId;
        SheetName = ImportGuard.Required(sheetName, 150, nameof(sheetName));
        RowNumber = rowNumber;
        RawPayload = ImportGuard.Json(rawPayload, 1_000_000, nameof(rawPayload));
        SKU = string.IsNullOrWhiteSpace(sku)
            ? null
            : SkuNormalizer.Normalize(ImportGuard.Required(sku, 100, nameof(sku)));
    }

    public Guid ImportJobId { get; private set; }

    public string SheetName { get; private set; } = string.Empty;

    public int RowNumber { get; private set; }

    public string? SKU { get; private set; }

    public string RawPayload { get; private set; } = string.Empty;

    public string? NormalizedPayload { get; private set; }

    public ImportRowStatus Status { get; private set; } = ImportRowStatus.Pending;

    public IReadOnlyCollection<ImportIssue> Issues => _issues;

    public void AddIssue(
        Guid issueId,
        ImportIssueSeverity severity,
        string code,
        string message,
        string? columnName = null,
        string? rawValue = null)
    {
        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity));
        }

        _issues.Add(
            new ImportIssue(
                issueId,
                Id,
                severity,
                code,
                message,
                columnName,
                rawValue));

        if (severity == ImportIssueSeverity.Error)
        {
            Status = ImportRowStatus.Invalid;
        }
    }

    public void MarkValid(string normalizedPayload)
    {
        if (_issues.Any(issue => issue.Severity == ImportIssueSeverity.Error))
        {
            throw new InvalidOperationException(
                "A row with validation errors cannot be marked valid.");
        }

        NormalizedPayload = ImportGuard.Json(
            normalizedPayload,
            1_000_000,
            nameof(normalizedPayload));
        Status = ImportRowStatus.Valid;
    }

    public void MarkPublished()
    {
        if (Status != ImportRowStatus.Valid)
        {
            throw new InvalidOperationException(
                "Only a valid import row can be marked published.");
        }

        Status = ImportRowStatus.Published;
    }

    public void MarkSkipped()
    {
        if (Status is not ImportRowStatus.Valid and not ImportRowStatus.Invalid)
        {
            throw new InvalidOperationException(
                "Only a validated import row can be skipped.");
        }

        Status = ImportRowStatus.Skipped;
    }
}
