namespace Ekiphan.Application.DataImport;

public sealed record ImportDeletionResult(int DeletedProducts, int RestoredPendingRelations, int TotalProducts);

public interface IImportDeletionService
{
    Task<ImportDeletionResult> DeleteAsync(Guid jobId, CancellationToken cancellationToken = default);
}
