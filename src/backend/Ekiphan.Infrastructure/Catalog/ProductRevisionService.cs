using System.Text.Json;
using Ekiphan.Application.Catalog;
using Ekiphan.Domain.Catalog;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Catalog;

public sealed class ProductSnapshotSerializer : IProductSnapshotSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public string Serialize(Product product)
    {
        var dto = new ProductRevisionSnapshotDto(
            SchemaVersion: 1,
            ProductId: product.Id,
            Sku: product.SKU,
            BrandId: product.BrandId,
            PrimaryCategoryId: product.PrimaryCategoryId,
            WorkflowStatus: product.WorkflowStatus,
            IsPublished: product.IsPublished,
            Categories: product.Categories.Select(c => new ProductCategorySnapshotDto(c.CategoryId, c.IsPrimary, c.SortOrder)).ToList(),
            Translations: product.Translations.Select(t => new ProductTranslationSnapshotDto(
                t.LanguageCode, t.Name, t.Slug, t.ShortDescription, t.LongDescription,
                t.MetaTitle, t.MetaDescription, t.CanonicalUrl, t.NoIndex, t.NoFollow,
                t.OpenGraphTitle, t.OpenGraphDescription, t.OpenGraphImageMediaId)).ToList(),
            Tags: product.Tags.Select(t => new ProductTagSnapshotDto(t.TagId, t.SortOrder)).ToList(),
            VariantGroups: product.VariantGroups.Select(vg => new ProductVariantGroupSnapshotDto(vg.Id, vg.Code, vg.SortOrder)).ToList(),
            Variants: product.Variants.Select(v => new ProductVariantSnapshotDto(v.Id, v.SKU, v.SortOrder, v.IsActive, v.MediaAssetId)).ToList());

        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    public ProductRevisionSnapshotDto Deserialize(string json)
    {
        return JsonSerializer.Deserialize<ProductRevisionSnapshotDto>(json, JsonOptions)
            ?? throw new InvalidOperationException("Invalid snapshot JSON.");
    }
}

public sealed class ProductRevisionService(
    EkiphanDbContext dbContext,
    IProductSnapshotSerializer serializer)
    : IProductRevisionService
{
    public async Task<ProductRevision> CreateRevisionAsync(
        Product product,
        ProductRevisionChangeType changeType,
        Guid actorUserId,
        string? reason = null,
        string? changedFieldsJson = null,
        ProductRevisionSource source = ProductRevisionSource.Manual,
        Guid? importBatchId = null,
        Guid? bulkOperationId = null,
        CancellationToken cancellationToken = default)
    {
        var snapshotJson = serializer.Serialize(product);
        var revision = new ProductRevision(
            Guid.NewGuid(),
            product.Id,
            product.VersionNumber,
            changeType,
            snapshotJson,
            changedFieldsJson,
            actorUserId,
            DateTimeOffset.UtcNow,
            reason,
            null,
            source,
            importBatchId,
            bulkOperationId);

        dbContext.ProductRevisions.Add(revision);
        await Task.CompletedTask;
        return revision;
    }

    public async Task<IReadOnlyList<ProductRevisionListItemDto>> GetRevisionsAsync(
        Guid productId,
        ProductRevisionChangeType? changeType = null,
        Guid? userId = null,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ProductRevisions.AsNoTracking()
            .Where(x => x.ProductId == productId);

        if (changeType.HasValue) query = query.Where(x => x.ChangeType == changeType.Value);
        if (userId.HasValue) query = query.Where(x => x.CreatedByUserId == userId.Value);
        if (dateFrom.HasValue) query = query.Where(x => x.CreatedAt >= dateFrom.Value);
        if (dateTo.HasValue) query = query.Where(x => x.CreatedAt <= dateTo.Value);

        return await query.OrderByDescending(x => x.VersionNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProductRevisionListItemDto(
                x.Id,
                x.VersionNumber,
                x.ChangeType,
                x.ChangedFieldsJson,
                x.CreatedByUserId,
                x.CreatedAt,
                x.Reason,
                x.Source))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductRevisionDetailDto?> GetRevisionAsync(
        Guid productId,
        Guid revisionId,
        CancellationToken cancellationToken = default)
    {
        var revision = await dbContext.ProductRevisions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == revisionId && x.ProductId == productId, cancellationToken);

        if (revision is null) return null;

        return new ProductRevisionDetailDto(
            revision.Id,
            revision.ProductId,
            revision.VersionNumber,
            revision.ChangeType,
            revision.SnapshotJson,
            revision.ChangedFieldsJson,
            revision.CreatedByUserId,
            revision.CreatedAt,
            revision.Reason,
            revision.CorrelationId,
            revision.Source);
    }
}

public sealed class ProductRevisionRestoreService(
    EkiphanDbContext dbContext,
    IProductSnapshotSerializer serializer,
    IProductRevisionService revisionService)
    : IProductRevisionRestoreService
{
    public async Task<ProductWorkflowTransitionResult> RestoreRevisionAsync(
        Guid productId,
        Guid revisionId,
        string reason,
        byte[] rowVersion,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .Include(x => x.Categories)
            .Include(x => x.Translations)
            .Include(x => x.Tags)
            .Include(x => x.Variants)
            .SingleOrDefaultAsync(x => x.Id == productId && !x.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException(ProductManagementErrorCodes.ProductNotFound);

        var revision = await dbContext.ProductRevisions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == revisionId && x.ProductId == productId, cancellationToken)
            ?? throw new KeyNotFoundException(ProductManagementErrorCodes.ProductRevisionNotFound);

        // Check concurrency
        if (product.RowVersion.Length > 0 && !product.RowVersion.SequenceEqual(rowVersion))
        {
            throw new DbUpdateConcurrencyException(ProductManagementErrorCodes.ProductConcurrencyConflict);
        }

        var snapshot = serializer.Deserialize(revision.SnapshotJson);

        // Check SKU conflict if SKU changed in snapshot
        if (!string.Equals(product.SKU, snapshot.Sku, StringComparison.OrdinalIgnoreCase))
        {
            var skuExists = await dbContext.Products.AsNoTracking()
                .AnyAsync(x => x.Id != productId && !x.IsDeleted && x.SKU == snapshot.Sku, cancellationToken);
            if (skuExists)
            {
                throw new InvalidOperationException(ProductManagementErrorCodes.ProductSkuDuplicate);
            }
        }

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Create pre-restore revision of current state
        await revisionService.CreateRevisionAsync(
            product,
            ProductRevisionChangeType.Restored,
            actorUserId,
            reason: $"Pre-restore snapshot before restoring version {revision.VersionNumber}.",
            source: ProductRevisionSource.Rollback,
            cancellationToken: cancellationToken);

        // Apply snapshot to product entity
        product.UpdateIdentity(snapshot.Sku, snapshot.BrandId);
        product.SetCategories(snapshot.Categories.Select(c => c.CategoryId).ToList(), snapshot.PrimaryCategoryId);
        
        foreach (var tr in snapshot.Translations)
        {
            product.SetTranslation(tr.LanguageCode, tr.Name, tr.Slug, tr.ShortDescription, tr.LongDescription,
                tr.MetaTitle, tr.MetaDescription, tr.CanonicalUrl, tr.NoIndex, tr.NoFollow,
                tr.OpenGraphTitle, tr.OpenGraphDescription, tr.OpenGraphImageMediaId);
        }

        product.SetTags(snapshot.Tags.Select(t => t.TagId).ToList());

        // Restore to Draft status by default
        product.SetWorkflowStatus(ProductWorkflowStatus.Draft, actorUserId, reason);
        product.IncrementVersion();

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new ProductWorkflowTransitionResult(true, product.WorkflowStatus);
    }
}
