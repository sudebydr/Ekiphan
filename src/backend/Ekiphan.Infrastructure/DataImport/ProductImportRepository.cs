using Ekiphan.Application.DataImport;
using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.DataImport;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.DataImport;

internal sealed class ProductImportRepository(EkiphanDbContext db) : IProductImportRepository
{
    public void AddBatch(ImportBatch batch) => db.ImportBatches.Add(batch);
    public Task<ImportBatch?> GetBatchAsync(Guid id, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<ImportBatch> query = db.ImportBatches.Include(x => x.Items);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
    public async Task<IReadOnlySet<string>> GetExistingSkusAsync(IEnumerable<string> skus, CancellationToken cancellationToken = default) =>
        (await db.Products.IgnoreQueryFilters().AsNoTracking()
            .Where(x => skus.Contains(x.SKU)).Select(x => x.SKU).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    public async Task<IReadOnlyDictionary<string, Guid>> GetBrandsAsync(IEnumerable<string> names, CancellationToken cancellationToken = default) =>
        (await db.Brands.AsNoTracking().Where(x => names.Contains(x.Name)).Select(x => new { x.Name, x.Id }).ToListAsync(cancellationToken))
            .ToDictionary(x => x.Name, x => x.Id, StringComparer.OrdinalIgnoreCase);
    public async Task<IReadOnlyDictionary<string, Guid>> GetCategoriesAsync(IEnumerable<string> names, CancellationToken cancellationToken = default)
    {
        var rows = await db.Set<CategoryTranslation>().AsNoTracking()
            .Where(x => x.LanguageCode == "tr" && names.Contains(x.Name))
            .Select(x => new { x.Name, x.CategoryId }).ToListAsync(cancellationToken);
        return rows.GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Where(x => x.Select(y => y.CategoryId).Distinct().Count() == 1)
            .ToDictionary(x => x.Key, x => x.First().CategoryId, StringComparer.OrdinalIgnoreCase);
    }
    public void AddProduct(Product product) => db.Products.Add(product);
    public Task<Product?> GetProductAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Products.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
    public async Task ExecuteTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
            await action(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        });
    }
}
