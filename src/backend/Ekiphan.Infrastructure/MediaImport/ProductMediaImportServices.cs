using System.Text;
using System.Security.Cryptography;
using Ekiphan.Application.Media;
using Ekiphan.Application.MediaImport;
using Ekiphan.Domain.Media;
using Ekiphan.Domain.Common;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#pragma warning disable CA1848, CA1826

namespace Ekiphan.Infrastructure.MediaImport;

internal static class ProductMediaImportExecutionPolicy
{
    public static bool IsImportable(ProductMediaImportFileValidationDto file) =>
        file.Status == ProductMediaValidationFileStatus.Valid && file.MatchedProductId.HasValue;
}

internal sealed class ProductMediaImportService(
    IProductMediaImportArchiveReader archiveReader,
    IProductMediaImportTokenService tokens,
    IProductMediaImportRepository repository,
    ITemporaryProductMediaStorage temporaryStorage,
    IValidator<ProductMediaImportPreviewRequest> validator,
    IOptions<ProductMediaImportOptions> options,
    TimeProvider clock,
    ILogger<ProductMediaImportService> logger) : IProductMediaImportService
{
    public async Task<ProductMediaImportPreviewDto> PreviewAsync(ProductMediaImportPreviewRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        if (request.Length > options.Value.MaxZipBytes) throw new ProductMediaImportSecurityException("ZIP size limit exceeded.");
        var container = Guid.NewGuid().ToString("N");
        try
        {
            var archive = await archiveReader.ReadAsync(container, request.Content, Path.GetFileName(request.FileName), request.ContentType, request.Length, cancellationToken);
            var batch = new ProductMediaImportBatch(Guid.NewGuid(), archive.FileName, archive.Sha256, request.UserId);
            await repository.AddBatchAsync(batch, cancellationToken);
            var upload = new ProductMediaStoredUpload(batch.Id, archive, request.UserId,
                clock.GetUtcNow().AddMinutes(options.Value.UploadTokenLifetimeMinutes));
            var token = tokens.CreateUpload(upload);
            var files = archive.Entries.Select(ToPreview).ToList();
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Product media preview completed. BatchId={BatchId} UserId={UserId} Status={Status}", batch.Id, request.UserId, batch.Status);
            return new(token, archive.FileName, archive.Length, archive.TotalEntries,
                files.Count(x => x.Status == ProductMediaPreviewFileStatus.Ready),
                files.Count(x => x.Status == ProductMediaPreviewFileStatus.Unsupported),
                files.Count(x => x.Status is ProductMediaPreviewFileStatus.Invalid or ProductMediaPreviewFileStatus.SecurityRejected),
                files.Count(x => x.Status == ProductMediaPreviewFileStatus.Duplicate),
                files.Count(x => x.Status == ProductMediaPreviewFileStatus.Skipped),
                files.Where(x => x.ExtractedSku is not null).Select(x => x.ExtractedSku).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                files, $"{files.Count} files analyzed; {files.Count(x => x.Status == ProductMediaPreviewFileStatus.Ready)} ready.");
        }
        catch { await temporaryStorage.DeleteAsync(container, CancellationToken.None); throw; }
    }

    public async Task<ProductMediaImportBatchDetailDto?> GetBatchAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        var batch = await repository.GetBatchAsync(batchId, false, cancellationToken); if (batch is null) return null;
        var items = new List<ProductMediaImportBatchItemDto>();
        foreach (var item in batch.Items)
            items.Add(new(item.OriginalFileName, item.ExtractedSku, item.ProductId,
                item.ProductId is { } id ? await repository.GetProductNameAsync(id, cancellationToken) : null,
                item.MediaAssetId, item.SortOrder, item.IsPrimary, item.Status, item.ErrorCode, item.ErrorMessage));
        return new(batch.Id, batch.FileName, batch.Status, batch.CreatedByUserId, batch.CreatedAt,
            batch.StartedAt, batch.CompletedAt, batch.TotalFiles, batch.MatchedFileCount, batch.ImportedFileCount,
            batch.SkippedFileCount, batch.ErrorFileCount,
            batch.Items.Count(x => x.Status == ProductMediaImportBatchItemStatus.Imported), items,
            batch.Items.Where(x => x.ErrorCode != null).GroupBy(x => x.ErrorCode!).ToDictionary(x => x.Key, x => x.Count()));
    }

    public async Task WriteErrorsCsvAsync(Guid batchId, Stream output, CancellationToken cancellationToken = default)
    {
        var detail = await GetBatchAsync(batchId, cancellationToken) ?? throw new KeyNotFoundException();
        await output.WriteAsync(Encoding.UTF8.GetPreamble(), cancellationToken);
        await using var writer = new StreamWriter(output, new UTF8Encoding(false), leaveOpen: true);
        await writer.WriteLineAsync("FileName,ExtractedSku,ProductId,ProductName,ErrorCode,ErrorMessage,Status");
        foreach (var x in detail.Items.Where(x => x.ErrorCode is not null))
            await writer.WriteLineAsync(string.Join(',', Csv(x.FileName), Csv(x.Sku), Csv(x.ProductId?.ToString()), Csv(x.ProductName), Csv(x.ErrorCode), Csv(x.ErrorMessage), Csv(x.Status.ToString())));
        await writer.FlushAsync(cancellationToken);
    }
    private static ProductMediaImportFilePreviewDto ToPreview(ProductMediaArchiveEntry x) => new(x.TemporaryFileId, x.OriginalFileName,
        x.NormalizedFileName, x.Extension, x.Length, x.Parsed.Sku, x.Parsed.Position, x.Parsed.SuggestedSortOrder,
        x.Parsed.SuggestedIsPrimary, x.Status, x.ErrorCode, x.ErrorMessage);
    private static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
}

internal sealed class ProductMediaImportValidationService(
    IProductMediaImportTokenService tokens,
    IProductMediaImportRepository repository,
    IProductMediaSkuResolver skuResolver,
    IValidator<ProductMediaImportValidateCommand> validator,
    IOptions<ProductMediaImportOptions> options,
    TimeProvider clock,
    ILogger<ProductMediaImportValidationService> logger) : IProductMediaImportValidationService
{
    public async Task<ProductMediaImportValidationResultDto> ValidateAsync(ProductMediaImportValidateCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        if (!tokens.TryGetUpload(command.UploadToken, out var upload)) throw new ProductMediaImportTokenException("Upload token is invalid or expired.");
        var batch = await repository.GetBatchAsync(upload.BatchId, true, cancellationToken) ?? throw new KeyNotFoundException();
        if (batch.Items.Count > 0) throw new ProductMediaImportConflictException("This upload has already been validated.");
        var manual = (command.ManualMappings ?? []).ToDictionary(x => x.TemporaryFileId, StringComparer.Ordinal);
        var importOptions = command.ImportOptions ?? new();
        var skus = upload.Archive.Entries.SelectMany(x =>
                manual.TryGetValue(x.TemporaryFileId, out var mapping) && !string.IsNullOrWhiteSpace(mapping.Sku)
                    ? [SkuNormalizer.Normalize(mapping.Sku)]
                    : skuResolver.GetCandidateSkus(x.NormalizedFileName))
            .ToArray();
        var ids = manual.Values.Where(x => x.ProductId.HasValue).Select(x => x.ProductId!.Value).ToArray();
        var products = await repository.FindProductsAsync(skus, ids, cancellationToken);
        var files = new List<ProductMediaImportFileValidationDto>();
        foreach (var entry in upload.Archive.Entries)
        {
            var errors = new List<ProductMediaImportErrorDto>(); var warnings = new List<ProductMediaImportWarningDto>();
            var status = entry.Status switch
            { ProductMediaPreviewFileStatus.Unsupported => ProductMediaValidationFileStatus.Ignored,
              ProductMediaPreviewFileStatus.Skipped => ProductMediaValidationFileStatus.Ignored,
              ProductMediaPreviewFileStatus.Duplicate => ProductMediaValidationFileStatus.Duplicate,
              ProductMediaPreviewFileStatus.Ready => ProductMediaValidationFileStatus.Valid,
              ProductMediaPreviewFileStatus.MissingSku => ProductMediaValidationFileStatus.Unmatched,
              _ => ProductMediaValidationFileStatus.Invalid };
            if (entry.Status == ProductMediaPreviewFileStatus.Duplicate && !importOptions.SkipDuplicateContent)
            { status = ProductMediaValidationFileStatus.Conflict; errors.Add(new("DUPLICATE_CONTENT", "contentHash", "Duplicate content exists in this ZIP.")); }
            ProductMediaProductMatch? product = null; var source = ProductMediaMatchSource.None;
            var mapping = manual.GetValueOrDefault(entry.TemporaryFileId);
            var candidates = mapping?.Sku is { Length: > 0 } manualSkuCandidates
                ? new[] { SkuNormalizer.Normalize(manualSkuCandidates) }
                : skuResolver.GetCandidateSkus(entry.NormalizedFileName);
            var resolution = mapping?.ProductId is { } productId
                ? new ProductMediaSkuResolution(products.SingleOrDefault(x => x.Id == productId), false)
                : mapping?.Sku is { Length: > 0 } manualSku
                    ? ResolveManualSku(manualSku, products)
                    : skuResolver.Resolve(entry.NormalizedFileName, products);
            if (mapping is not null) source = ProductMediaMatchSource.Manual;
            else if (entry.Parsed.Sku is not null) source = ProductMediaMatchSource.FileName;
            if (status is ProductMediaValidationFileStatus.Valid or ProductMediaValidationFileStatus.Unmatched)
            {
                if (mapping?.ProductId is null && candidates.Count == 0)
                { status = ProductMediaValidationFileStatus.Unmatched; errors.Add(CreateResolutionFailure(false)); }
                else if (resolution.IsAmbiguous) { status = ProductMediaValidationFileStatus.Conflict; errors.Add(new("AMBIGUOUS_SKU", "sku", "The SKU matches more than one product.")); }
                else if (resolution.Product is null) { status = importOptions.SkipUnmatchedFiles ? ProductMediaValidationFileStatus.Unmatched : ProductMediaValidationFileStatus.Conflict; errors.Add(CreateResolutionFailure(true)); }
                else if (resolution.Product.IsDeleted) { status = ProductMediaValidationFileStatus.Invalid; errors.Add(new("PRODUCT_DELETED", "productId", "The selected product is deleted.")); }
                else product = resolution.Product;
            }
            if (entry.Status is ProductMediaPreviewFileStatus.Invalid or ProductMediaPreviewFileStatus.SecurityRejected)
                errors.Add(new(entry.ErrorCode ?? "INVALID_FILE", "file", entry.ErrorMessage ?? "The file is invalid."));
            else if (entry.Status is ProductMediaPreviewFileStatus.Skipped or ProductMediaPreviewFileStatus.Unsupported)
                warnings.Add(new(entry.ErrorCode ?? "UNSUPPORTED_MEDIA_ENTRY", "file", entry.ErrorMessage ?? "The file was skipped."));
            var sort = mapping?.SortOrder ?? entry.Parsed.SuggestedSortOrder;
            var primary = mapping?.IsPrimary ?? entry.Parsed.SuggestedIsPrimary;
            if (product?.HasPrimaryImage == true && primary && !importOptions.ReplaceExistingPrimaryImage)
            { primary = false; warnings.Add(new("PRIMARY_PRESERVED", "isPrimary", "The existing primary image will be preserved.")); }
            files.Add(new(entry.TemporaryFileId, entry.OriginalFileName, entry.Parsed.Sku, product?.Id, product?.Sku,
                product?.Name, source, sort, primary, entry.Sha256, status, errors, warnings));
        }
        var hashes = files.Where(x => x.ContentHash.Length == 64).Select(x => x.ContentHash)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var matchedProductIds = files.Where(x => x.Status == ProductMediaValidationFileStatus.Valid && x.MatchedProductId.HasValue)
            .Select(x => x.MatchedProductId!.Value).Distinct().ToArray();
        var existingAssets = await repository.FindMediaAssetsByHashesAsync(hashes, cancellationToken);
        var existingLinks = await repository.FindProductMediaContentLinksAsync(matchedProductIds, hashes, cancellationToken);
        ApplyContentDuplicateRules(files, existingAssets, existingLinks, importOptions.SkipDuplicateContent);
        NormalizeProductAssignments(files);
        foreach (var file in files)
        {
            var entry = upload.Archive.Entries.Single(x => x.TemporaryFileId == file.TemporaryFileId);
            var item = new ProductMediaImportBatchItem(Guid.NewGuid(), batch.Id, file.TemporaryFileId, file.OriginalFileName,
                file.ExtractedSku, file.ContentHash, file.SortOrder, file.IsPrimary);
            if (ProductMediaImportExecutionPolicy.IsImportable(file)) item.Match(file.MatchedProductId!.Value, file.SortOrder, file.IsPrimary);
            else item.SetFailure(file.Status switch { ProductMediaValidationFileStatus.Duplicate => ProductMediaImportBatchItemStatus.Duplicate,
                ProductMediaValidationFileStatus.Ignored or ProductMediaValidationFileStatus.Unmatched => ProductMediaImportBatchItemStatus.Skipped,
                _ => ProductMediaImportBatchItemStatus.Failed }, file.Errors.FirstOrDefault()?.Code ?? file.Status.ToString().ToUpperInvariant(),
                file.Errors.FirstOrDefault()?.Message ?? file.Warnings.FirstOrDefault()?.Message ?? file.Status.ToString());
            batch.AddItem(item);
        }
        var matched = files.Count(ProductMediaImportExecutionPolicy.IsImportable);
        var errorsCount = files.Count(x => x.Status is ProductMediaValidationFileStatus.Invalid or ProductMediaValidationFileStatus.Conflict);
        batch.SetValidationSummary(
    files.Count,
    matched,
    files.Count - matched - errorsCount,
    errorsCount);

try
{
    await repository.SaveAsync(cancellationToken);
}
catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
{
    foreach (var entry in ex.Entries)
    {
        Console.WriteLine(
            $"CONCURRENCY ENTITY: {entry.Metadata.ClrType.FullName} | " +
            $"STATE: {entry.State} | " +
            $"VALUES: {string.Join(", ", entry.Properties.Select(p => $"{p.Metadata.Name}={p.CurrentValue}"))}");
    }

    throw;
}
        var validation = new ProductMediaStoredValidation(batch.Id, command.UploadToken, importOptions, files,
            clock.GetUtcNow().AddMinutes(options.Value.ValidationTokenLifetimeMinutes));
        var validationToken = tokens.CreateValidation(validation);
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Product media validation completed. BatchId={BatchId} UserId={UserId} Status={Status}", batch.Id, upload.UserId, batch.Status);
        return new(validationToken, files.Count, matched, files.Count(x => x.Status == ProductMediaValidationFileStatus.Unmatched),
            files.Count(x => x.Status == ProductMediaValidationFileStatus.Duplicate), files.Count(x => x.Status == ProductMediaValidationFileStatus.Invalid),
            files.Count(x => x.Status == ProductMediaValidationFileStatus.Ignored), files.Where(x => x.MatchedProductId.HasValue).Select(x => x.MatchedProductId).Distinct().Count(),
            products.Count(x => !x.HasPrimaryImage), files.Count(x => x.Status == ProductMediaValidationFileStatus.Conflict), files,
            $"{matched} files matched; {errorsCount} files contain critical errors.");
    }

    private static ProductMediaSkuResolution ResolveManualSku(string sku, IReadOnlyCollection<ProductMediaProductMatch> products)
    {
        var canonicalSku = SkuNormalizer.Normalize(sku);
        var matches = products.Where(product => SkuNormalizer.Normalize(product.Sku) == canonicalSku).ToArray();
        return matches.Length == 1 ? new(matches[0], false) : new(null, matches.Length > 1);
    }

    internal static ProductMediaImportErrorDto CreateResolutionFailure(bool hasCandidate) => hasCandidate
        ? new("PRODUCT_NOT_FOUND", "sku", "No editable product matches the SKU.")
        : new("SKU_RESOLUTION_FAILED", "sku", "A safe SKU could not be extracted from the file name.");

    internal static void ApplyContentDuplicateRules(
        List<ProductMediaImportFileValidationDto> files,
        IReadOnlyDictionary<string, Guid> existingAssets,
        IReadOnlySet<ProductMediaContentLink> existingLinks,
        bool skipDuplicateContent)
    {
        var pendingLinks = new HashSet<ProductMediaContentLink>();
        for (var index = 0; index < files.Count; index++)
        {
            var file = files[index];
            if (file.Status != ProductMediaValidationFileStatus.Valid || file.MatchedProductId is not { } productId ||
                file.ContentHash.Length != 64)
                continue;

            var link = new ProductMediaContentLink(productId, file.ContentHash);
            if (existingLinks.Contains(link) || !pendingLinks.Add(link))
            {
                const string message = "The same image is already assigned to this product.";
                files[index] = file with
                {
                    Status = skipDuplicateContent ? ProductMediaValidationFileStatus.Duplicate : ProductMediaValidationFileStatus.Conflict,
                    Errors = skipDuplicateContent ? file.Errors : [.. file.Errors, new("DUPLICATE_MEDIA", "contentHash", message)],
                    Warnings = skipDuplicateContent ? [.. file.Warnings, new("DUPLICATE_MEDIA", "contentHash", message)] : file.Warnings
                };
                continue;
            }

            files[index] = file with
            {
                ExistingMediaAssetId = existingAssets.TryGetValue(file.ContentHash, out var assetId)
                    ? assetId
                    : null
            };
        }
    }

    private static void NormalizeProductAssignments(List<ProductMediaImportFileValidationDto> files)
    {
        foreach (var group in files.Where(x => x.Status == ProductMediaValidationFileStatus.Valid && x.MatchedProductId.HasValue).GroupBy(x => x.MatchedProductId))
        {
            var ordered = group.OrderBy(x => x.SortOrder).ThenBy(x => x.OriginalFileName, StringComparer.OrdinalIgnoreCase).ToList();
            var preserveExisting = ordered.Any(x => x.Warnings.Any(w => w.Code == "PRIMARY_PRESERVED"));
            var primary = preserveExisting ? null : ordered.Where(x => x.IsPrimary)
                .OrderBy(PrimaryPriority).ThenBy(x => x.SortOrder).FirstOrDefault() ?? ordered[0];
            for (var i = 0; i < ordered.Count; i++)
            {
                var index = files.IndexOf(ordered[i]);
                files[index] = ordered[i] with { SortOrder = i + 1, IsPrimary = primary is not null && ordered[i].TemporaryFileId == primary.TemporaryFileId };
            }
        }
    }

    private static int PrimaryPriority(ProductMediaImportFileValidationDto file)
    {
        if (file.MatchSource == ProductMediaMatchSource.Manual) return 0;
        var stem = Path.GetFileNameWithoutExtension(file.OriginalFileName);
        if (stem.EndsWith("-main", StringComparison.OrdinalIgnoreCase) || stem.EndsWith("_main", StringComparison.OrdinalIgnoreCase)) return 1;
        if (stem.EndsWith("-1", StringComparison.OrdinalIgnoreCase) || stem.EndsWith("_1", StringComparison.OrdinalIgnoreCase)) return 2;
        return 3;
    }
}

internal sealed class ProductMediaImportExecutionService(
    IProductMediaImportTokenService tokens,
    IProductMediaImportRepository repository,
    ITemporaryProductMediaStorage temporaryStorage,
    MediaUploadService mediaUpload,
    IMediaFileStorage mediaStorage,
    IValidator<ProductMediaImportExecuteCommand> validator,
    TimeProvider clock,
    ILogger<ProductMediaImportExecutionService> logger) : IProductMediaImportExecutionService
{
    public async Task<ProductMediaImportExecutionResultDto> ExecuteAsync(ProductMediaImportExecuteCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        if (!tokens.TryUseValidation(command.ValidationToken, out var validation)) throw new ProductMediaImportTokenException("Validation token is invalid, expired, or already used.");
        if (!tokens.TryGetUpload(validation.UploadToken, out var upload)) throw new ProductMediaImportTokenException("Source upload is invalid or expired.");
        if (!string.Equals(upload.Archive.Sha256, (await repository.GetBatchAsync(validation.BatchId, false, cancellationToken))?.OriginalFileHash, StringComparison.OrdinalIgnoreCase))
            throw new ProductMediaImportConflictException("ZIP hash changed after preview.");
        var batch = await repository.GetBatchAsync(validation.BatchId, true, cancellationToken) ?? throw new KeyNotFoundException();
        var errors = new List<ProductMediaImportErrorDto>(); var imported = 0; var skipped = 0; var failed = 0;
        var storageKeysToCompensate = new HashSet<string>(StringComparer.Ordinal);
        var assetsByHash = validation.Files.Where(x => x.ExistingMediaAssetId is { } id && id != Guid.Empty)
            .GroupBy(x => x.ContentHash, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.MinBy(file => file.ExistingMediaAssetId!.Value)!.ExistingMediaAssetId!.Value,
                StringComparer.OrdinalIgnoreCase);
        var transactionCommitted = false;
        var now = clock.GetUtcNow(); batch.Start(now); await repository.SaveAsync(cancellationToken);
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Product media execute started. BatchId={BatchId} UserId={UserId}", batch.Id, upload.UserId);
        try
        {
            await repository.ExecuteInTransactionAsync(async ct =>
            {
                foreach (var file in validation.Files)
                {
                    var item = batch.Items.Single(x => x.TemporaryFileId == file.TemporaryFileId);
                    if (!ProductMediaImportExecutionPolicy.IsImportable(file)) { skipped++; continue; }
                    UploadedMediaResult? uploaded = null;
                    try
                    {
                        await using var content = await temporaryStorage.OpenReadAsync(upload.Archive.ContainerId, file.TemporaryFileId, ct);
                        var archiveEntry = upload.Archive.Entries.Single(x => x.TemporaryFileId == file.TemporaryFileId);
                        var currentHash = Convert.ToHexString(await SHA256.HashDataAsync(content, ct));
                        if (!string.Equals(currentHash, file.ContentHash, StringComparison.OrdinalIgnoreCase))
                            throw new ProductMediaImportConflictException("A temporary image changed after validation.");
                        content.Position = 0;
                        if (assetsByHash.TryGetValue(file.ContentHash, out var existingAssetId))
                        {
                            uploaded = new(existingAssetId, MediaAssetType.Image, archiveEntry.NormalizedFileName, string.Empty,
                                archiveEntry.ContentType, archiveEntry.Length, file.ContentHash);
                        }
                        else
                        {
                            uploaded = await mediaUpload.UploadAsync(new(content, Path.GetFileName(archiveEntry.NormalizedFileName), archiveEntry.ContentType,
                                archiveEntry.Length, MediaAssetType.Image, "tr", file.MatchedProductName ?? file.MatchedProductSku ?? "Ürün görseli",
                                file.MatchedProductName ?? file.MatchedProductSku ?? "Ürün görseli"), ct);
                            storageKeysToCompensate.Add(uploaded.StorageKey);
                            assetsByHash[file.ContentHash] = uploaded.Id;
                        }
                        await repository.AddProductMediaAsync(file.MatchedProductId!.Value, uploaded.Id, batch.Id, file.SortOrder,
                            file.IsPrimary, validation.Options.ReplaceExistingPrimaryImage, now, ct);
                        item.Imported(uploaded.Id, now); imported++;
                        if (logger.IsEnabled(LogLevel.Information))
                            logger.LogInformation("Product media file imported. BatchId={BatchId} ProductId={ProductId} SKU={SKU} Status=Imported", batch.Id, file.MatchedProductId, file.MatchedProductSku);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException and
                        not ProductMediaImportConflictException and not MediaUploadUnavailableException)
                    {
                        failed++; item.SetFailure(ProductMediaImportBatchItemStatus.Failed, "IMPORT_FAILED", ex.Message);
                        errors.Add(new("IMPORT_FAILED", file.OriginalFileName, ex.Message));
                        if (uploaded is not null)
                        {
                            var key = await repository.ArchiveUnassignedMediaAsync(uploaded.Id, now, ct);
                            if (key is not null) await mediaStorage.DeleteAsync(key, CancellationToken.None);
                        }
                        logger.LogWarning(ex, "Product media file skipped. BatchId={BatchId} ProductId={ProductId} SKU={SKU} Status=Failed", batch.Id, file.MatchedProductId, file.MatchedProductSku);
                    }
                }
                batch.Complete(clock.GetUtcNow(), imported, skipped, failed);
            }, cancellationToken);
            transactionCommitted = true;
            await temporaryStorage.DeleteAsync(upload.Archive.ContainerId, CancellationToken.None);
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Product media batch completed. BatchId={BatchId} UserId={UserId} Status={Status}", batch.Id, upload.UserId, batch.Status);
            return new(batch.Id, batch.Status, imported, skipped, failed, errors);
        }
        catch (Exception ex)
        {
            if (!transactionCommitted)
            {
                foreach (var storageKey in storageKeysToCompensate)
                {
                    try
                    {
                        await mediaStorage.DeleteAsync(storageKey, CancellationToken.None);
                    }
                    catch (Exception cleanupException)
                    {
                        logger.LogWarning(cleanupException,
                            "Product media storage compensation failed. BatchId={BatchId} StorageKey={StorageKey}",
                            batch.Id, storageKey);
                    }
                }
            }
            batch.Fail(clock.GetUtcNow(), ex.Message, compensationRequired: true);
            try { await repository.SaveAsync(CancellationToken.None); } catch { }
            logger.LogError(ex, "Product media batch failed. BatchId={BatchId} UserId={UserId} Status={Status}", batch.Id, upload.UserId, batch.Status);
            throw;
        }
    }
}

internal sealed class ProductMediaImportRollbackService(
    IProductMediaImportRepository repository,
    IValidator<ProductMediaImportRollbackCommand> validator,
    TimeProvider clock,
    ILogger<ProductMediaImportRollbackService> logger) : IProductMediaImportRollbackService
{
    public async Task<ProductMediaImportRollbackResultDto> RollbackAsync(ProductMediaImportRollbackCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var batch = await repository.GetBatchAsync(command.BatchId, true, cancellationToken) ?? throw new KeyNotFoundException();
        if (batch.Status == ProductMediaImportBatchStatus.RolledBack) throw new ProductMediaImportConflictException("Batch has already been rolled back.");
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Product media rollback started. BatchId={BatchId} UserId={UserId}", batch.Id, command.UserId);
        var rolledBack = 0; var skipped = 0; var archived = 0; var errors = new List<ProductMediaImportErrorDto>();
        await repository.ExecuteInTransactionAsync(async ct =>
        {
            foreach (var item in batch.Items.Where(x => x.Status == ProductMediaImportBatchItemStatus.Imported))
            {
                var result = await repository.RollbackItemAsync(item, clock.GetUtcNow(), ct);
                if (result.Removed) { item.MarkRolledBack(); rolledBack++; if (result.ArchiveAsset) archived++; }
                else { item.MarkRollbackSkipped("The product-media link changed after import or is already absent."); skipped++; }
            }
            batch.MarkRolledBack(clock.GetUtcNow(), command.UserId);
        }, cancellationToken);
        var skippedItems = new List<ProductMediaImportBatchItemDto>();
        foreach (var item in batch.Items.Where(x => x.Status == ProductMediaImportBatchItemStatus.RollbackSkipped))
            skippedItems.Add(new(item.OriginalFileName, item.ExtractedSku, item.ProductId,
                item.ProductId is { } id ? await repository.GetProductNameAsync(id, cancellationToken) : null,
                item.MediaAssetId, item.SortOrder, item.IsPrimary, item.Status, item.ErrorCode, item.ErrorMessage));
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Product media rollback completed. BatchId={BatchId} UserId={UserId} Status={Status}", batch.Id, command.UserId, batch.Status);
        return new(batch.Id, rolledBack, skipped, archived, 0, errors, skippedItems);
    }
}
