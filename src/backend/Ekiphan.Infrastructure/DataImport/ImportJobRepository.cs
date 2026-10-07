using Ekiphan.Application.DataImport;
using Ekiphan.Domain.DataImport;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.DataImport;

internal sealed class ImportJobRepository(EkiphanDbContext dbContext)
    : IImportJobRepository
{
    public Task<bool> SourceExistsAsync(
        string sourceSha256Checksum,
        CancellationToken cancellationToken = default) =>
        dbContext.ImportJobs.AnyAsync(
            job => !job.IsDryRun && job.SourceSha256Checksum == sourceSha256Checksum &&
                (job.Status == ImportJobStatus.ReadyToPublish ||
                 job.Status == ImportJobStatus.Publishing ||
                 job.Status == ImportJobStatus.Completed),
            cancellationToken);

    public void Add(ImportJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        dbContext.ImportJobs.Add(job);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
