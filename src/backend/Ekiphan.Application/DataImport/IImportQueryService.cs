using Ekiphan.Domain.DataImport;

namespace Ekiphan.Application.DataImport;

public interface IImportQueryService
{
    Task<PagedResult<ImportJobSummary>> GetJobsAsync(
        int page,
        int pageSize,
        ImportJobStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<ImportJobDetail?> GetJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ImportIssueItem>?> GetIssuesAsync(
        Guid jobId,
        int page,
        int pageSize,
        ImportIssueSeverity? severity = null,
        CancellationToken cancellationToken = default);
}
