using Ekiphan.Domain.DataImport;

namespace Ekiphan.Application.DataImport;

public interface IImportIssueReportSource
{
    IAsyncEnumerable<ImportIssueItem> StreamAsync(
        Guid jobId,
        ImportIssueSeverity? severity = null,
        CancellationToken cancellationToken = default);
}
