using Ekiphan.Application.DataImport;
using Ekiphan.Domain.DataImport;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.DataImport;

internal sealed class ImportQueryService(EkiphanDbContext dbContext)
    : IImportQueryService
{
    public async Task<PagedResult<ImportJobSummary>> GetJobsAsync(
        int page,
        int pageSize,
        ImportJobStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(page, pageSize);
        var query = dbContext.ImportJobs.AsNoTracking();
        if (status.HasValue)
        {
            query = query.Where(job => job.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(job => job.CreatedAt)
            .ThenByDescending(job => job.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(job => new ImportJobSummary(
                job.Id,
                job.OriginalFileName,
                job.SourceType,
                job.Status,
                job.IsDryRun,
                job.TotalRowCount,
                job.ValidRowCount,
                job.InvalidRowCount,
                job.WarningCount,
                job.PublishedRowCount,
                job.CreatedAt,
                job.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<ImportJobSummary>(
            items,
            page,
            pageSize,
            totalCount);
    }

    public Task<ImportJobDetail?> GetJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        dbContext.ImportJobs
            .AsNoTracking()
            .Where(job => job.Id == jobId)
            .Select(job => new ImportJobDetail(
                job.Id,
                job.OriginalFileName,
                job.SourceType,
                job.Status,
                job.IsDryRun,
                job.SourceSha256Checksum,
                job.CreatedByUserId,
                job.TotalRowCount,
                job.ValidRowCount,
                job.InvalidRowCount,
                job.WarningCount,
                job.PublishedRowCount,
                job.ValidationStartedAt,
                job.ValidationCompletedAt,
                job.PublishingStartedAt,
                job.CompletedAt,
                job.FailureReason,
                job.CreatedAt,
                job.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<ImportIssueItem>?> GetIssuesAsync(
        Guid jobId,
        int page,
        int pageSize,
        ImportIssueSeverity? severity = null,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(page, pageSize);
        if (!await dbContext.ImportJobs
                .AsNoTracking()
                .AnyAsync(job => job.Id == jobId, cancellationToken))
        {
            return null;
        }

        var query = dbContext.Set<ImportRow>()
            .AsNoTracking()
            .Where(row => row.ImportJobId == jobId)
            .SelectMany(
                row => row.Issues,
                (row, issue) => new
                {
                    Row = row,
                    Issue = issue,
                });
        if (severity.HasValue)
        {
            query = query.Where(item => item.Issue.Severity == severity.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(item => item.Row.SheetName)
            .ThenBy(item => item.Row.RowNumber)
            .ThenByDescending(item => item.Issue.Severity)
            .ThenBy(item => item.Issue.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new ImportIssueItem(
                item.Issue.Id,
                item.Row.SheetName,
                item.Row.RowNumber,
                item.Row.SKU,
                item.Issue.Severity,
                item.Issue.Code,
                item.Issue.Message,
                item.Issue.ColumnName,
                item.Issue.RawValue))
            .ToListAsync(cancellationToken);

        return new PagedResult<ImportIssueItem>(
            items,
            page,
            pageSize,
            totalCount);
    }

    private static void ValidatePagination(int page, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 100);
    }
}
