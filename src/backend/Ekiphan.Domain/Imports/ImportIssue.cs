using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.DataImport;

public sealed class ImportIssue : Entity
{
    private ImportIssue()
    {
    }

    internal ImportIssue(
        Guid id,
        Guid importRowId,
        ImportIssueSeverity severity,
        string code,
        string message,
        string? columnName,
        string? rawValue)
        : base(id)
    {
        ImportRowId = importRowId;
        Severity = severity;
        Code = ImportGuard.Required(code, 100, nameof(code)).ToUpperInvariant();
        Message = ImportGuard.Required(message, 2000, nameof(message));
        ColumnName = string.IsNullOrWhiteSpace(columnName)
            ? null
            : ImportGuard.Required(columnName, 150, nameof(columnName));
        RawValue = string.IsNullOrWhiteSpace(rawValue)
            ? null
            : ImportGuard.Required(rawValue, 2000, nameof(rawValue));
    }

    public Guid ImportRowId { get; private set; }

    public ImportIssueSeverity Severity { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public string? ColumnName { get; private set; }

    public string? RawValue { get; private set; }
}
