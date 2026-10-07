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

    Task<IReadOnlyDictionary<string, Product>> GetProductsBySkusAsync(
        IReadOnlyCollection<string> skus,
        CancellationToken cancellationToken = default);

    void AddProduct(Product product);

    void AddAttributeValue(ProductAttributeValue value);

    void AddIssue(ImportIssue issue);

    Task PrepareProductDataAsync(
        IReadOnlyCollection<Guid> productIds,
        IReadOnlyCollection<string> attributeCodes,
        IReadOnlyCollection<Guid> materialAttributeIds,
        CancellationToken cancellationToken = default);

    Task ReplaceImportedAttributesAsync(Guid productId,
        IReadOnlyDictionary<string, string[]> values,
        CancellationToken cancellationToken = default);

    Task ReplaceMaterialAsync(Guid productId, Guid? attributeId, Guid? optionId, string? rawValue,
        CancellationToken cancellationToken = default);

    Task UpsertVariantAsync(Product product, string variantKey, int sortOrder,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ApplyRelationsAsync(Guid sourceProductId,
        IReadOnlyCollection<string> similarSkus,
        IReadOnlyCollection<string> complementarySkus,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> ApplyRelationsBatchAsync(
        IReadOnlyCollection<ImportRelationRequest> requests,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}

public sealed record ImportRelationRequest(
    Guid RowId,
    Guid SourceProductId,
    IReadOnlyCollection<string> SimilarSkus,
    IReadOnlyCollection<string> ComplementarySkus);
