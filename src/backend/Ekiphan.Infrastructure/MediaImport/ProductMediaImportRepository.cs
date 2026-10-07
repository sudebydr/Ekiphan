using Ekiphan.Application.MediaImport;
using Ekiphan.Domain.Media;
using Ekiphan.Domain.Common;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.MediaImport;

internal sealed class ProductMediaImportRepository(EkiphanDbContext db) : IProductMediaImportRepository
{
    public async Task AddBatchAsync(ProductMediaImportBatch batch, CancellationToken cancellationToken = default)
    { db.Add(batch); await db.SaveChangesAsync(cancellationToken); }
    public async Task SaveAsync(CancellationToken cancellationToken = default)
{
    foreach (var entry in db.ChangeTracker
                 .Entries<ProductMediaImportBatchItem>()
                 .Where(x => x.State == EntityState.Modified))
    {
        var exists = await db.Set<ProductMediaImportBatchItem>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == entry.Entity.Id, cancellationToken);

        if (!exists)
        {
            entry.State = EntityState.Added;
        }
    }

    await db.SaveChangesAsync(cancellationToken);
}

    public async Task<IReadOnlyList<ProductMediaProductMatch>> FindProductsAsync(IReadOnlyCollection<string> skus,
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        var normalized = skus.Select(SkuNormalizer.Normalize).Distinct(StringComparer.Ordinal).ToArray();
        return await db.Products.AsNoTracking().Where(x => ids.Contains(x.Id) || normalized.Contains(x.NormalizedSku))
            .Select(x => new ProductMediaProductMatch(x.Id, x.SKU,
                x.Translations.Where(t => t.LanguageCode == "tr").Select(t => t.Name).FirstOrDefault() ?? x.SKU,
                x.IsDeleted, x.IsPublished, x.UpdatedAt,
                db.ProductMedia.Any(m => m.ProductId == x.Id && m.Role == ProductMediaRole.GalleryImage && m.IsDefault)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, Guid>> FindMediaAssetsByHashesAsync(IReadOnlyCollection<string> hashes,
        CancellationToken cancellationToken = default) =>
        (await db.MediaAssets.AsNoTracking().Where(x => x.Sha256Checksum != null && hashes.Contains(x.Sha256Checksum))
            .Select(x => new { Hash = x.Sha256Checksum!, x.Id }).ToListAsync(cancellationToken))
            .GroupBy(x => x.Hash, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.MinBy(asset => asset.Id)!.Id, StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlySet<ProductMediaContentLink>> FindProductMediaContentLinksAsync(
        IReadOnlyCollection<Guid> productIds, IReadOnlyCollection<string> hashes,
        CancellationToken cancellationToken = default) =>
        (await (from link in db.ProductMedia.AsNoTracking()
                join asset in db.MediaAssets.AsNoTracking() on link.MediaAssetId equals asset.Id
                where productIds.Contains(link.ProductId) && link.Role == ProductMediaRole.GalleryImage &&
                      asset.Sha256Checksum != null && hashes.Contains(asset.Sha256Checksum)
                select new ProductMediaContentLink(link.ProductId, asset.Sha256Checksum!)).ToListAsync(cancellationToken))
            .ToHashSet();

    public Task<ProductMediaImportBatch?> GetBatchAsync(Guid id, bool tracked, CancellationToken cancellationToken = default)
    {
        var query = db.Set<ProductMediaImportBatch>().Include(x => x.Items).AsQueryable();
        if (!tracked) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<string?> GetProductNameAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Products.AsNoTracking().Where(x => x.Id == id).Select(x => x.Translations
            .Where(t => t.LanguageCode == "tr").Select(t => t.Name).FirstOrDefault() ?? x.SKU).SingleOrDefaultAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await action(cancellationToken); await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
    }

    public async Task AddProductMediaAsync(Guid productId, Guid mediaAssetId, Guid batchId, int sortOrder,
        bool isPrimary, bool replaceExistingPrimary, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        ProductMedia? current = null;
        if (isPrimary)
        {
            current = await db.ProductMedia.SingleOrDefaultAsync(x => x.ProductId == productId &&
                x.Role == ProductMediaRole.GalleryImage && x.IsDefault, cancellationToken);
            if (current is not null)
            {
                if (!replaceExistingPrimary) isPrimary = false;
                else current.Update(false, current.SortOrder);
            }
        }
        var link = new ProductMedia(productId, mediaAssetId, ProductMediaRole.GalleryImage, isPrimary, sortOrder);
        link.MarkImported(batchId, now); db.ProductMedia.Add(link);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch
        {
            db.Entry(link).State = EntityState.Detached;
            if (current is not null) await db.Entry(current).ReloadAsync(cancellationToken);
            throw;
        }
    }

    public async Task<(bool Removed, bool ArchiveAsset, string? StorageKey)> RollbackItemAsync(
        ProductMediaImportBatchItem item, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (item.ProductId is null || item.MediaAssetId is null) return (false, false, null);
        var link = await db.ProductMedia.SingleOrDefaultAsync(x => x.ProductId == item.ProductId &&
            x.MediaAssetId == item.MediaAssetId && x.Role == ProductMediaRole.GalleryImage, cancellationToken);
        if (link is null || link.CreatedByImportBatchId != item.BatchId || item.ProductVersionAtImport is null ||
            link.UpdatedAt != item.ProductVersionAtImport) return (false, false, null);
        var restorePrimary = link.IsDefault;
        db.ProductMedia.Remove(link);
        var usedElsewhere = await db.ProductMedia.AnyAsync(x => x.MediaAssetId == item.MediaAssetId && x.ProductId != item.ProductId, cancellationToken)
            || await db.CategoryMedia.AnyAsync(x => x.MediaAssetId == item.MediaAssetId, cancellationToken)
            || await db.BrandMedia.AnyAsync(x => x.MediaAssetId == item.MediaAssetId, cancellationToken);
        var asset = !usedElsewhere ? await db.MediaAssets.SingleOrDefaultAsync(x => x.Id == item.MediaAssetId, cancellationToken) : null;
        await db.SaveChangesAsync(cancellationToken);
        if (restorePrimary)
        {
            var fallback = await db.ProductMedia.Where(x => x.ProductId == item.ProductId && x.Role == ProductMediaRole.GalleryImage)
                .OrderBy(x => x.SortOrder).FirstOrDefaultAsync(cancellationToken);
            if (fallback is not null) { fallback.Update(true, fallback.SortOrder); await db.SaveChangesAsync(cancellationToken); }
        }
        if (asset is not null && asset.Status != MediaStatus.Archived) { asset.Archive(now); await db.SaveChangesAsync(cancellationToken); }
        return (true, asset is not null, asset?.StorageKey);
    }

    public async Task<string?> ArchiveUnassignedMediaAsync(Guid mediaAssetId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (await db.ProductMedia.AnyAsync(x => x.MediaAssetId == mediaAssetId, cancellationToken)) return null;
        var asset = await db.MediaAssets.SingleOrDefaultAsync(x => x.Id == mediaAssetId, cancellationToken);
        if (asset is null) return null; asset.Archive(now); await db.SaveChangesAsync(cancellationToken); return asset.StorageKey;
    }
}
