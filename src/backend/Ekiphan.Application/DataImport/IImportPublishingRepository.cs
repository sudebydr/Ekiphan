using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.DataImport;

namespace Ekiphan.Application.DataImport;

public interface IImportPublishingRepository
{
    Task<ImportJob?> GetJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<HashSet<string>> GetExistingSkusAsync(
        IReadOnlyCollection<string> skus,
        CancellationToken cancellationToken = default);

    void AddProduct(Product product);

    void AddAttributeValue(ProductAttributeValue value);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}
