using Ekiphan.Domain.DataImport;

namespace Ekiphan.Application.DataImport;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record ImportJobSummary(
    Guid Id,
    string? OriginalFileName,
    ImportSourceType SourceType,
    ImportJobStatus Status,
    bool IsDryRun,
    int TotalRowCount,
    int ValidRowCount,
    int InvalidRowCount,
    int WarningCount,
    int PublishedRowCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ImportJobDetail(
    Guid Id,
    string? OriginalFileName,
    ImportSourceType SourceType,
    ImportJobStatus Status,
    bool IsDryRun,
    string SourceSha256Checksum,
    Guid? CreatedByUserId,
    int TotalRowCount,
    int ValidRowCount,
    int InvalidRowCount,
    int WarningCount,
    int PublishedRowCount,
    DateTimeOffset? ValidationStartedAt,
    DateTimeOffset? ValidationCompletedAt,
    DateTimeOffset? PublishingStartedAt,
    DateTimeOffset? CompletedAt,
    string? FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ImportIssueItem(
    Guid Id,
    string SheetName,
    int RowNumber,
    string? SKU,
    ImportIssueSeverity Severity,
    string Code,
    string Message,
    string? ColumnName,
    string? RawValue);
