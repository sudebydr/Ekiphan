using System.Globalization;
using System.Text;
using System.Text.Json;
using Ekiphan.Application.Catalog;
using Ekiphan.Domain.Catalog;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Catalog;

public sealed class ProductBulkOperationService(
    EkiphanDbContext dbContext,
    IProductBulkOperationQueue queue,
    IProductRevisionService revisionService,
    IProductQualityService qualityService)
    : IProductBulkOperationService
{
    private const int SynchronousLimit = 50;
    private const int OperationItemInsertBatchSize = 100;

    public async Task<ProductBulkPreviewResultDto> PreviewAsync(
        ProductBulkPreviewCommand command,
        CancellationToken cancellationToken = default)
    {
        var products = await dbContext.Products.AsNoTracking()
            .Include(x => x.Categories)
            .Include(x => x.Translations)
            .Where(x => command.ProductIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var eligible = new List<Guid>();
        var skipped = 0;
        var invalid = 0;
        var warnings = new List<string>();
        var validationErrors = new List<string>();

        foreach (var productId in command.ProductIds)
        {
            var p = products.FirstOrDefault(x => x.Id == productId);
            if (p is null || p.IsDeleted)
            {
                invalid++;
                validationErrors.Add($"Product ID '{productId}' was not found or is soft-deleted.");
                continue;
            }

            if (p.WorkflowStatus == ProductWorkflowStatus.Archived && command.OperationType != ProductBulkOperationType.RestoreToDraft)
            {
                skipped++;
                warnings.Add($"Product '{p.SKU}' is archived and will be skipped.");
                continue;
            }

            eligible.Add(p.Id);
        }

        var previewToken = Guid.NewGuid().ToString("N");

        return new ProductBulkPreviewResultDto(
            TotalProducts: command.ProductIds.Count,
            EligibleProducts: eligible.Count,
            SkippedProducts: skipped,
            InvalidProducts: invalid,
            Warnings: warnings,
            ValidationErrors: validationErrors,
            AffectedProductIds: eligible,
            PreviewToken: previewToken,
            ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(15));
    }

    public async Task<ProductBulkExecutionResultDto> ExecuteAsync(
        ProductBulkExecuteCommand command,
        CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid();
        var payloadJson = command.Parameters is not null ? JsonSerializer.Serialize(command.Parameters) : null;

        var bulkOp = new ProductBulkOperation(
            operationId,
            command.OperationType,
            command.ActorUserId,
            DateTimeOffset.UtcNow,
            command.ProductIds.Count,
            payloadJson);

        dbContext.ProductBulkOperations.Add(bulkOp);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Persist the operation items in bounded batches. A single SaveChanges for a
        // large selection can produce an oversized INSERT command and hold locks long
        // enough to time out under normal admin traffic.
        foreach (var productIdBatch in command.ProductIds.Chunk(OperationItemInsertBatchSize))
        {
            foreach (var productId in productIdBatch)
            {
                bulkOp.AddItem(Guid.NewGuid(), productId);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        bool isBackground = command.ExecuteAsBackgroundJob || command.ProductIds.Count > SynchronousLimit;

        if (isBackground)
        {
            await queue.EnqueueAsync(operationId, cancellationToken);
            return new ProductBulkExecutionResultDto(
                operationId,
                ProductBulkOperationStatus.Pending,
                IsBackgroundJob: true,
                TotalCount: bulkOp.TotalItemCount,
                SuccessCount: 0,
                FailedCount: 0,
                SkippedCount: 0,
                ErrorMessage: null);
        }

        // Execute synchronously in batches of 100
        await ProcessBulkOperationCoreAsync(operationId, cancellationToken);

        var updatedOp = await dbContext.ProductBulkOperations.AsNoTracking()
            .SingleAsync(x => x.Id == operationId, cancellationToken);

        return new ProductBulkExecutionResultDto(
            operationId,
            updatedOp.Status,
            IsBackgroundJob: false,
            TotalCount: updatedOp.TotalItemCount,
            SuccessCount: updatedOp.SuccessCount,
            FailedCount: updatedOp.FailedCount,
            SkippedCount: updatedOp.SkippedCount,
            ErrorMessage: updatedOp.ErrorMessage);
    }

    public async Task ProcessBulkOperationCoreAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        var op = await dbContext.ProductBulkOperations
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == operationId, cancellationToken);

        if (op is null || op.Status == ProductBulkOperationStatus.Completed || op.Status == ProductBulkOperationStatus.Failed)
        {
            return;
        }

        op.Start(DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        int success = op.SuccessCount, failed = op.FailedCount, skipped = op.SkippedCount;
        int batchSize = 100;

        var itemsList = op.Items
            .Where(item => item.Status == ProductBulkOperationItemStatus.Pending)
            .ToList();
        for (int i = 0; i < itemsList.Count; i += batchSize)
        {
            var batch = itemsList.Skip(i).Take(batchSize).ToList();
            var productIds = batch.Select(b => b.ProductId).ToList();

            var products = await dbContext.Products
                .Include(p => p.Categories)
                .Include(p => p.Translations)
                .Include(p => p.Tags)
                .Include(p => p.Variants)
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            foreach (var item in batch)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product is null || product.IsDeleted)
                {
                    skipped++;
                    item.MarkSkipped("Product not found or soft-deleted.", DateTimeOffset.UtcNow);
                    continue;
                }

                try
                {
                    var prevVersion = product.VersionNumber;
                    var actionResult = await ApplyBulkActionAsync(
                        op.OperationType,
                        product,
                        op.RequestPayloadJson,
                        op.RequestedByUserId,
                        cancellationToken);
                    if (actionResult.Updated)
                    {
                        product.IncrementVersion();
                        item.MarkCompleted(prevVersion, product.VersionNumber, DateTimeOffset.UtcNow);
                        success++;

                        await revisionService.CreateRevisionAsync(
                            product,
                            ProductRevisionChangeType.BulkUpdated,
                            op.RequestedByUserId,
                            reason: $"Bulk operation {op.OperationType} executed.",
                            source: ProductRevisionSource.BulkOperation,
                            bulkOperationId: op.Id,
                            cancellationToken: cancellationToken);
                    }
                    else if (actionResult.ErrorCode is not null)
                    {
                        failed++;
                        item.MarkFailed(actionResult.ErrorCode, actionResult.ErrorMessage!, DateTimeOffset.UtcNow);
                    }
                    else
                    {
                        skipped++;
                        item.MarkSkipped("No changes applied or action skipped.", DateTimeOffset.UtcNow);
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    item.MarkFailed("BULK_ITEM_ERROR", ex.Message, DateTimeOffset.UtcNow);
                }
            }

            op.UpdateProgress(success, failed, skipped);
            await dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }

        op.Complete(DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<BulkActionResult> ApplyBulkActionAsync(
        ProductBulkOperationType type,
        Product product,
        string? payloadJson,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (type == ProductBulkOperationType.Publish)
        {
            if (product.WorkflowStatus is not (ProductWorkflowStatus.InReview or ProductWorkflowStatus.Draft))
            {
                return BulkActionResult.NoChanges;
            }

            var quality = await qualityService.EvaluateAsync(product.Id, cancellationToken);
            if (quality.BlocksPublishing)
            {
                return new BulkActionResult(
                    Updated: false,
                    ErrorCode: ProductManagementErrorCodes.ProductQualityBlocksPublishing,
                    ErrorMessage: BuildQualityBlockingMessage(quality));
            }

            product.SetWorkflowStatus(ProductWorkflowStatus.Published, actorUserId);
            return BulkActionResult.UpdatedSuccessfully;
        }

        return ApplyBulkAction(type, product, payloadJson, actorUserId)
            ? BulkActionResult.UpdatedSuccessfully
            : BulkActionResult.NoChanges;
    }

    private static string BuildQualityBlockingMessage(ProductQualityResultDto quality)
    {
        var blockingIssues = quality.Issues
            .Where(issue => issue.BlocksPublishing)
            .Select(issue => $"{issue.Code}: {issue.Message}")
            .ToList();

        return blockingIssues.Count > 0
            ? string.Join(" | ", blockingIssues)
            : "Product quality evaluation blocks publishing.";
    }

    private static bool ApplyBulkAction(ProductBulkOperationType type, Product product, string? payloadJson, Guid actorUserId)
    {
        switch (type)
        {
            case ProductBulkOperationType.Publish:
                if (product.WorkflowStatus is ProductWorkflowStatus.InReview or ProductWorkflowStatus.Draft)
                {
                    product.SetWorkflowStatus(ProductWorkflowStatus.Published, actorUserId);
                    return true;
                }
                return false;

            case ProductBulkOperationType.Unpublish:
                if (product.WorkflowStatus == ProductWorkflowStatus.Published)
                {
                    product.SetWorkflowStatus(ProductWorkflowStatus.Unpublished, actorUserId);
                    return true;
                }
                return false;

            case ProductBulkOperationType.Archive:
                if (product.WorkflowStatus != ProductWorkflowStatus.Archived)
                {
                    product.SetWorkflowStatus(ProductWorkflowStatus.Archived, actorUserId, "Bulk archived.");
                    return true;
                }
                return false;

            case ProductBulkOperationType.ChangeBrand:
                if (!string.IsNullOrWhiteSpace(payloadJson))
                {
                    using var doc = JsonDocument.Parse(payloadJson);
                    if (doc.RootElement.TryGetProperty("brandId", out var brandProp) && Guid.TryParse(brandProp.GetString(), out var brandId))
                    {
                        product.UpdateIdentity(product.SKU, brandId);
                        return true;
                    }
                }
                return false;

            case ProductBulkOperationType.AddCategories:
                if (!string.IsNullOrWhiteSpace(payloadJson))
                {
                    using var doc = JsonDocument.Parse(payloadJson);
                    if (doc.RootElement.TryGetProperty("categoryIds", out var catProp) && catProp.ValueKind == JsonValueKind.Array)
                    {
                        var categoryIds = catProp.EnumerateArray().Select(x => Guid.Parse(x.GetString()!)).ToList();
                        var current = product.Categories.Select(c => c.CategoryId).ToList();
                        var merged = current.Union(categoryIds).ToList();
                        product.SetCategories(merged, product.PrimaryCategoryId);
                        return true;
                    }
                }
                return false;

            case ProductBulkOperationType.SubmitForReview:
                if (product.WorkflowStatus == ProductWorkflowStatus.Draft)
                {
                    product.SetWorkflowStatus(ProductWorkflowStatus.InReview, actorUserId);
                    return true;
                }
                return false;

            case ProductBulkOperationType.RestoreToDraft:
                if (product.WorkflowStatus != ProductWorkflowStatus.Draft)
                {
                    product.SetWorkflowStatus(ProductWorkflowStatus.Draft, actorUserId);
                    return true;
                }
                return false;

            default:
                return false;
        }
    }

    private sealed record BulkActionResult(bool Updated, string? ErrorCode, string? ErrorMessage)
    {
        public static readonly BulkActionResult UpdatedSuccessfully = new(true, null, null);
        public static readonly BulkActionResult NoChanges = new(false, null, null);
    }

    public async Task<ProductBulkOperationDetailDto?> GetOperationAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        var op = await dbContext.ProductBulkOperations.AsNoTracking()
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == operationId, cancellationToken);

        if (op is null) return null;

        var progress = op.TotalItemCount == 0 ? 100.0 : Math.Round((double)(op.SuccessCount + op.FailedCount + op.SkippedCount) / op.TotalItemCount * 100, 2);

        return new ProductBulkOperationDetailDto(
            op.Id,
            op.OperationType,
            op.Status,
            op.TotalItemCount,
            op.SuccessCount,
            op.FailedCount,
            op.SkippedCount,
            op.RequestedByUserId,
            op.RequestedAt,
            op.StartedAt,
            op.CompletedAt,
            progress,
            op.ErrorMessage,
            op.Items.Select(i => new ProductBulkOperationItemDto(
                i.Id,
                i.ProductId,
                i.Status,
                i.ErrorCode,
                i.ErrorMessage,
                i.PreviousVersionNumber,
                i.NewVersionNumber,
                i.CompletedAt ?? DateTimeOffset.UtcNow)).ToList());
    }

    public async Task<byte[]> ExportErrorsCsvAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        var op = await dbContext.ProductBulkOperations.AsNoTracking()
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == operationId, cancellationToken)
            ?? throw new KeyNotFoundException(ProductManagementErrorCodes.ProductBulkOperationNotFound);

        var failedItems = op.Items.Where(x => x.Status == ProductBulkOperationItemStatus.Failed || x.Status == ProductBulkOperationItemStatus.Skipped).ToList();
        var productIds = failedItems.Select(x => x.ProductId).ToList();

        var products = await dbContext.Products.AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("ProductId,SKU,ProductName,OperationType,ErrorCode,ErrorMessage,Status");

        foreach (var item in failedItems)
        {
            products.TryGetValue(item.ProductId, out var product);
            var sku = product?.SKU ?? "N/A";
            var name = product?.Translations.FirstOrDefault(t => t.LanguageCode == "tr")?.Name ?? "N/A";

            sb.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\"",
                Escape(item.ProductId.ToString()),
                Escape(sku),
                Escape(name),
                Escape(op.OperationType.ToString()),
                Escape(item.ErrorCode ?? "SKIPPED"),
                Escape(item.ErrorMessage ?? "N/A"),
                Escape(item.Status.ToString())));
        }

        var preamble = Encoding.UTF8.GetPreamble();
        var bodyBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[preamble.Length + bodyBytes.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(bodyBytes, 0, result, preamble.Length, bodyBytes.Length);

        return result;
    }

    private static string Escape(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var sanitized = input.Replace("\"", "\"\"");
        if (sanitized.StartsWith('=') || sanitized.StartsWith('+') || sanitized.StartsWith('-') || sanitized.StartsWith('@'))
        {
            sanitized = "'" + sanitized;
        }
        return sanitized;
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Queue suffix is domain expressive.")]
public sealed class InMemoryProductBulkOperationQueue : IProductBulkOperationQueue
{
    private readonly System.Collections.Concurrent.ConcurrentQueue<Guid> _queue = new();

    public Task EnqueueAsync(Guid bulkOperationId, CancellationToken cancellationToken = default)
    {
        _queue.Enqueue(bulkOperationId);
        return Task.CompletedTask;
    }

    public bool TryDequeue(out Guid bulkOperationId)
    {
        return _queue.TryDequeue(out bulkOperationId);
    }
}
