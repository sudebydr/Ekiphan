using Ekiphan.Application.DataImport;
using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.DataImport;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.DataImport;

internal sealed class ImportPublishingRepository(EkiphanDbContext dbContext)
    : IImportPublishingRepository
{
    private const int SkuQueryBatchSize = 1_000;

    public Task<ImportJob?> GetJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        dbContext.ImportJobs
            .Include(job => job.Rows)
            .ThenInclude(row => row.Issues)
            .SingleOrDefaultAsync(job => job.Id == jobId, cancellationToken);

    public async Task<HashSet<string>> GetExistingSkusAsync(
        IReadOnlyCollection<string> skus,
        CancellationToken cancellationToken = default)
    {
        var existingSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var batch in skus
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Chunk(SkuQueryBatchSize))
        {
            var matches = await dbContext.Products
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(product => batch.Contains(product.SKU))
                .Select(product => product.SKU)
                .ToListAsync(cancellationToken);
            existingSkus.UnionWith(matches);
        }

        return existingSkus;
    }

    public void AddProduct(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        dbContext.Products.Add(product);
    }

    public void AddAttributeValue(ProductAttributeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        dbContext.ProductAttributeValues.Add(value);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(
            async () =>
            {
                await using var transaction =
                    await dbContext.Database.BeginTransactionAsync(
                        cancellationToken);
                await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            });
    }
}
