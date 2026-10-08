using System.Data;
using Ekiphan.Application.DataImport;
using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.DataImport;
using Ekiphan.Domain.Quotes;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.DataImport;

internal sealed class ImportDeletionService(EkiphanDbContext db) : IImportDeletionService
{
    public async Task<ImportDeletionResult> DeleteAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        // Retrying execution strategies must replay the entire transaction, never individual deletes.
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = db.Database.IsRelational()
                ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
                : await db.Database.BeginTransactionAsync(cancellationToken);
            var job = await db.ImportJobs.Include(item => item.Rows).ThenInclude(row => row.Issues)
                .SingleOrDefaultAsync(item => item.Id == jobId, cancellationToken)
                ?? throw new ImportJobNotFoundException(jobId);
            if (job.Status is ImportJobStatus.Uploaded or ImportJobStatus.Validating or ImportJobStatus.Publishing)
                throw new InvalidOperationException("İşlenmekte olan import silinemez. İşlemin tamamlanmasını bekleyin.");

            // Separate legacy batch imports cannot safely be attributed by checksum alone.
            if (await db.ImportBatches.AnyAsync(batch => batch.OriginalFileHash == job.SourceSha256Checksum, cancellationToken))
                throw new InvalidOperationException("Bu kaynak ayrı bir batch importuyla da kullanılmış. Sahiplik güvenilir belirlenemediği için silme engellendi.");

            var skus = job.Rows.Where(row => row.Status == ImportRowStatus.Published && row.SKU != null)
                .Select(row => Ekiphan.Domain.Common.SkuNormalizer.Normalize(row.SKU!)).Distinct().ToArray();
            var matched = await db.Products.IgnoreQueryFilters()
                .Where(product => skus.Contains(product.NormalizedSku) || product.CreatedByImportJobId == jobId)
                .ToListAsync(cancellationToken);
            if (matched.Any(product => product.CreatedByImportJobId == null))
                throw new InvalidOperationException("Ürünlerin ilk oluşturulduğu import kaydı mevcut değil. Sahiplik güvenilir belirlenemediği için silme engellendi; hiçbir kayıt silinmedi.");
            var owned = matched.Where(product => product.CreatedByImportJobId == jobId).ToArray();
            var ids = owned.Select(product => product.Id).ToArray();
            var ownedSkus = owned.Select(product => product.NormalizedSku).ToArray();
            var otherRows = await db.Set<ImportRow>().Where(row => row.ImportJobId != jobId &&
                    row.Status == ImportRowStatus.Published && row.SKU != null)
                .Select(row => row.SKU!).ToListAsync(cancellationToken);
            if (otherRows.Select(Ekiphan.Domain.Common.SkuNormalizer.Normalize).Intersect(ownedSkus).Any() ||
                await db.ImportBatchItems.AnyAsync(item => item.ProductId.HasValue && ids.Contains(item.ProductId.Value), cancellationToken))
                throw new InvalidOperationException("Bu importun ürünleri başka bir importta da kullanılmış. Diğer importları korumak için silme engellendi.");

            var relations = await db.ProductRelations
                .Where(relation => ids.Contains(relation.SourceProductId) || ids.Contains(relation.TargetProductId))
                .ToListAsync(cancellationToken);
            var incoming = relations.Where(relation => !ids.Contains(relation.SourceProductId) &&
                relation.SourceProductId != relation.TargetProductId).ToArray();
            var targetSkus = owned.ToDictionary(product => product.Id, product => product.NormalizedSku);
            var pending = await db.PendingProductRelations.Where(item => ownedSkus.Contains(item.TargetNormalizedSku))
                .ToListAsync(cancellationToken);
            var pendingKeys = pending.ToDictionary(item => (item.SourceProductId, item.TargetNormalizedSku, item.RelationType));
            var restored = 0;
            foreach (var relation in incoming)
            {
                var sku = targetSkus[relation.TargetProductId];
                var key = (relation.SourceProductId, sku, relation.RelationType);
                if (pendingKeys.TryGetValue(key, out var existing))
                {
                    if (existing.SortOrder != relation.SortOrder)
                        throw new InvalidOperationException("Pending ilişki sıralaması çakışıyor. Hiçbir kayıt silinmedi.");
                    continue;
                }
                var item = new PendingProductRelation(relation.SourceProductId, sku, relation.RelationType, relation.SortOrder);
                db.PendingProductRelations.Add(item);
                pendingKeys.Add(key, item);
                restored++;
            }
            db.ProductRelations.RemoveRange(relations);
            db.PendingProductRelations.RemoveRange(await db.PendingProductRelations
                .Where(item => ids.Contains(item.SourceProductId)).ToListAsync(cancellationToken));

            var variants = await db.ProductVariants.Where(item => ids.Contains(item.ProductId)).Select(item => item.Id).ToListAsync(cancellationToken);
            // Preserve quote and other import histories; only detach their nullable product links.
            foreach (var item in await db.Set<QuoteRequestItem>().Where(item =>
                (item.ProductId.HasValue && ids.Contains(item.ProductId.Value)) ||
                (item.VariantId.HasValue && variants.Contains(item.VariantId.Value))).ToListAsync(cancellationToken))
            {
                if (item.ProductId.HasValue && ids.Contains(item.ProductId.Value)) db.Entry(item).Property(x => x.ProductId).CurrentValue = null;
                if (item.VariantId.HasValue && variants.Contains(item.VariantId.Value)) db.Entry(item).Property(x => x.VariantId).CurrentValue = null;
            }
            foreach (var item in await db.ProductMediaImportBatchItems.Where(item => item.ProductId.HasValue && ids.Contains(item.ProductId.Value)).ToListAsync(cancellationToken))
                db.Entry(item).Property(x => x.ProductId).CurrentValue = null;
            foreach (var item in await db.ShowroomHotspots.Where(item => item.ProductId.HasValue && ids.Contains(item.ProductId.Value)).ToListAsync(cancellationToken))
                db.Entry(item).Property(x => x.ProductId).CurrentValue = null;
            db.ReferenceProjectProducts.RemoveRange(await db.ReferenceProjectProducts.Where(item => ids.Contains(item.ProductId)).ToListAsync(cancellationToken));
            db.ProductBulkOperationItems.RemoveRange(await db.ProductBulkOperationItems.Where(item => ids.Contains(item.ProductId)).ToListAsync(cancellationToken));
            // Flush pending restoration and restrictive FK detachment before cascading product deletes.
            await db.SaveChangesAsync(cancellationToken);
            await DeleteAsync(db.Set<ProductVariantSelection>().Where(item => variants.Contains(item.ProductVariantId)), cancellationToken);
            await DeleteAsync(db.Products.IgnoreQueryFilters().Where(product => ids.Contains(product.Id)), cancellationToken);
            foreach (var product in owned) db.Entry(product).State = EntityState.Detached;
            db.ImportJobs.Remove(job);
            await db.SaveChangesAsync(cancellationToken);
            var total = await db.Products.IgnoreQueryFilters().CountAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new ImportDeletionResult(ids.Length, restored, total);
        });
    }

    private async Task DeleteAsync<T>(IQueryable<T> query, CancellationToken cancellationToken) where T : class
    {
        if (db.Database.IsRelational()) await query.ExecuteDeleteAsync(cancellationToken);
        else { db.RemoveRange(await query.ToListAsync(cancellationToken)); await db.SaveChangesAsync(cancellationToken); }
    }
}
