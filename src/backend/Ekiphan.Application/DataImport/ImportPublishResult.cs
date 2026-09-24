using Ekiphan.Domain.DataImport;

namespace Ekiphan.Application.DataImport;

public sealed record ImportPublishResult(
    Guid JobId,
    ImportJobStatus Status,
    int PublishedRowCount,
    int SkippedRowCount);
