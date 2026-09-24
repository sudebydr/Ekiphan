using Ekiphan.Domain.DataImport;

namespace Ekiphan.Application.DataImport;

public interface IImportJobRepository
{
    Task<bool> SourceExistsAsync(
        string sourceSha256Checksum,
        CancellationToken cancellationToken = default);

    void Add(ImportJob job);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
