using Ekiphan.Domain.DataImport;

namespace Ekiphan.Application.DataImport;

public interface IImportIssueReportWriter
{
    Task WriteCsvAsync(
        Guid jobId,
        Stream destination,
        ImportIssueSeverity? severity = null,
        CancellationToken cancellationToken = default);
}
