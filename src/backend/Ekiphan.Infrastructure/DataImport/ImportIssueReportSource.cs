using Ekiphan.Application.DataImport;
using Ekiphan.Domain.DataImport;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.DataImport;

internal sealed class ImportIssueReportSource(EkiphanDbContext dbContext)
    : IImportIssueReportSource
{
    public IAsyncEnumerable<ImportIssueItem> StreamAsync(
        Guid jobId,
        ImportIssueSeverity? severity = null,
        CancellationToken cancellationToken = default)
    {
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

        return query
            .OrderBy(item => item.Row.SheetName)
            .ThenBy(item => item.Row.RowNumber)
            .ThenByDescending(item => item.Issue.Severity)
            .ThenBy(item => item.Issue.Code)
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
            .AsAsyncEnumerable();
    }
}
