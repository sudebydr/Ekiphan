using Ekiphan.Domain.DataImport;

namespace Ekiphan.Api.DataImport;

public sealed record ImportJobResponse(
    Guid Id,
    string FileName,
    ImportSourceType SourceType,
    ImportJobStatus Status,
    bool IsDryRun,
    int TotalRowCount,
    int ValidRowCount,
    int InvalidRowCount,
    int WarningCount,
    string? FailureReason);
