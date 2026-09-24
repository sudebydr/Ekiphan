using System.Text;
using System.Security.Cryptography;
using Ekiphan.Application.Media;
using Ekiphan.Application.MediaImport;
using Ekiphan.Domain.Media;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#pragma warning disable CA1848, CA1826

namespace Ekiphan.Infrastructure.MediaImport;

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
        var skus = upload.Archive.Entries.Select(x => manual.TryGetValue(x.TemporaryFileId, out var m) ? m.Sku : x.Parsed.Sku)
            .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim().ToUpperInvariant()).ToArray();
        var ids = manual.Values.Where(x => x.ProductId.HasValue).Select(x => x.ProductId!.Value).ToArray();
        var products = await repository.FindProductsAsync(skus, ids, cancellationToken);
        var existingHashes = await repository.ExistingMediaHashesAsync(upload.Archive.Entries.Where(x => x.Sha256.Length == 64).Select(x => x.Sha256).ToArray(), cancellationToken);
        var files = new List<ProductMediaImportFileValidationDto>();
        foreach (var entry in upload.Archive.Entries)
        {
            var errors = new List<ProductMediaImportErrorDto>(); var warnings = new List<ProductMediaImportWarningDto>();
            var status = entry.Status switch
            { ProductMediaPreviewFileStatus.Unsupported => ProductMediaValidationFileStatus.Ignored,
              ProductMediaPreviewFileStatus.Duplicate => ProductMediaValidationFileStatus.Duplicate,
              ProductMediaPreviewFileStatus.Ready => ProductMediaValidationFileStatus.Valid,
              ProductMediaPreviewFileStatus.MissingSku => ProductMediaValidationFileStatus.Unmatched,
              _ => ProductMediaValidationFileStatus.Invalid };
            if (entry.Status == ProductMediaPreviewFileStatus.Duplicate && !importOptions.SkipDuplicateContent)
            { status = ProductMediaValidationFileStatus.Conflict; errors.Add(new("DUPLICATE_CONTENT", "contentHash", "Duplicate content exists in this ZIP.")); }
            ProductMediaProductMatch? product = null; var source = ProductMediaMatchSource.None;
            var mapping = manual.GetValueOrDefault(entry.TemporaryFileId);
            var requestedSku = mapping?.Sku?.Trim().ToUpperInvariant() ?? entry.Parsed.Sku;
            var matches = mapping?.ProductId is { } productId
                ? products.Where(x => x.Id == productId).ToList()
                : products.Where(x => string.Equals(x.Sku, requestedSku, StringComparison.OrdinalIgnoreCase)).ToList();
            if (mapping is not null) source = ProductMediaMatchSource.Manual;
            else if (entry.Parsed.Sku is not null) source = ProductMediaMatchSource.FileName;
            if (status is ProductMediaValidationFileStatus.Valid or ProductMediaValidationFileStatus.Unmatched)
            {
                if (matches.Count == 0) { status = importOptions.SkipUnmatchedFiles ? ProductMediaValidationFileStatus.Unmatched : ProductMediaValidationFileStatus.Conflict; errors.Add(new("PRODUCT_NOT_FOUND", "sku", "No editable product matches the SKU.")); }
                else if (matches.Count > 1) { status = ProductMediaValidationFileStatus.Conflict; errors.Add(new("AMBIGUOUS_SKU", "sku", "The SKU matches more than one product.")); }
                else if (matches[0].IsDeleted) { status = ProductMediaValidationFileStatus.Invalid; errors.Add(new("PRODUCT_DELETED", "productId", "The selected product is deleted.")); }
                else product = matches[0];
            }
            if (entry.Sha256.Length == 64 && existingHashes.Contains(entry.Sha256))
            {
                status = importOptions.SkipDuplicateContent ? ProductMediaValidationFileStatus.Duplicate : ProductMediaValidationFileStatus.Conflict;
                if (importOptions.SkipDuplicateContent)
                    warnings.Add(new("MEDIA_EXISTS", "contentHash", "The same content already exists in the media library."));
                else
                    errors.Add(new("MEDIA_EXISTS", "contentHash", "The same content already exists in the media library."));
            }
            if (entry.Status is ProductMediaPreviewFileStatus.Invalid or ProductMediaPreviewFileStatus.SecurityRejected)
                errors.Add(new(entry.ErrorCode ?? "INVALID_FILE", "file", entry.ErrorMessage ?? "The file is invalid."));
            var sort = mapping?.SortOrder ?? entry.Parsed.SuggestedSortOrder;
            var primary = mapping?.IsPrimary ?? entry.Parsed.SuggestedIsPrimary;
            if (product?.HasPrimaryImage == true && primary && !importOptions.ReplaceExistingPrimaryImage)
            { primary = false; warnings.Add(new("PRIMARY_PRESERVED", "isPrimary", "The existing primary image will be preserved.")); }
            files.Add(new(entry.TemporaryFileId, entry.OriginalFileName, entry.Parsed.Sku, product?.Id, product?.Sku,
                product?.Name, source, sort, primary, entry.Sha256, status, errors, warnings));
        }
        NormalizeProductAssignments(files);
        foreach (var file in files)
        {
            var entry = upload.Archive.Entries.Single(x => x.TemporaryFileId == file.TemporaryFileId);
            var item = new ProductMediaImportBatchItem(Guid.NewGuid(), batch.Id, file.TemporaryFileId, file.OriginalFileName,
                file.ExtractedSku, file.ContentHash, file.SortOrder, file.IsPrimary);
            if (file.Status == ProductMediaValidationFileStatus.Valid && file.MatchedProductId is { } id) item.Match(id, file.SortOrder, file.IsPrimary);
            else item.SetFailure(file.Status switch { ProductMediaValidationFileStatus.Duplicate => ProductMediaImportBatchItemStatus.Duplicate,
                ProductMediaValidationFileStatus.Ignored or ProductMediaValidationFileStatus.Unmatched => ProductMediaImportBatchItemStatus.Skipped,
                _ => ProductMediaImportBatchItemStatus.Failed }, file.Errors.FirstOrDefault()?.Code ?? file.Status.ToString().ToUpperInvariant(),
                file.Errors.FirstOrDefault()?.Message ?? file.Warnings.FirstOrDefault()?.Message ?? file.Status.ToString());
            batch.AddItem(item);
        }
        var matched = files.Count(x => x.Status == ProductMediaValidationFileStatus.Valid);
        var errorsCount = files.Count(x => x.Status is ProductMediaValidationFileStatus.Invalid or ProductMediaValidationFileStatus.Conflict);
        batch.SetValidationSummary(files.Count, matched, files.Count - matched - errorsCount, errorsCount); await repository.SaveAsync(cancellationToken);
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
        if (validation.Files.Any(x => x.Status is ProductMediaValidationFileStatus.Invalid or ProductMediaValidationFileStatus.Conflict))
            throw new ProductMediaImportConflictException("Validation contains critical errors.");
        if (!string.Equals(upload.Archive.Sha256, (await repository.GetBatchAsync(validation.BatchId, false, cancellationToken))?.OriginalFileHash, StringComparison.OrdinalIgnoreCase))
            throw new ProductMediaImportConflictException("ZIP hash changed after preview.");
        var batch = await repository.GetBatchAsync(validation.BatchId, true, cancellationToken) ?? throw new KeyNotFoundException();
        var errors = new List<ProductMediaImportErrorDto>(); var imported = 0; var skipped = 0;
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
                    if (file.Status != ProductMediaValidationFileStatus.Valid || file.MatchedProductId is null) { skipped++; continue; }
                    UploadedMediaResult? uploaded = null;
                    try
                    {
                        await using var content = await temporaryStorage.OpenReadAsync(upload.Archive.ContainerId, file.TemporaryFileId, ct);
                        var archiveEntry = upload.Archive.Entries.Single(x => x.TemporaryFileId == file.TemporaryFileId);
                        var currentHash = Convert.ToHexString(await SHA256.HashDataAsync(content, ct));
                        if (!string.Equals(currentHash, file.ContentHash, StringComparison.OrdinalIgnoreCase))
                            throw new ProductMediaImportConflictException("A temporary image changed after validation.");
                        content.Position = 0;
                        uploaded = await mediaUpload.UploadAsync(new(content, Path.GetFileName(file.OriginalFileName), archiveEntry.ContentType,
                            archiveEntry.Length, MediaAssetType.Image, "tr", file.MatchedProductName ?? file.MatchedProductSku ?? "Ürün görseli",
                            file.MatchedProductName ?? file.MatchedProductSku ?? "Ürün görseli"), ct);
                        await repository.AddProductMediaAsync(file.MatchedProductId.Value, uploaded.Id, batch.Id, file.SortOrder,
                            file.IsPrimary, validation.Options.ReplaceExistingPrimaryImage, now, ct);
                        item.Imported(uploaded.Id, now); imported++;
                        if (logger.IsEnabled(LogLevel.Information))
                            logger.LogInformation("Product media file imported. BatchId={BatchId} ProductId={ProductId} SKU={SKU} Status=Imported", batch.Id, file.MatchedProductId, file.MatchedProductSku);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException and
                        not ProductMediaImportConflictException and not MediaUploadUnavailableException)
                    {
                        skipped++; item.SetFailure(ProductMediaImportBatchItemStatus.Failed, "IMPORT_FAILED", ex.Message);
                        errors.Add(new("IMPORT_FAILED", file.OriginalFileName, ex.Message));
                        if (uploaded is not null)
                        {
                            var key = await repository.ArchiveUnassignedMediaAsync(uploaded.Id, now, ct);
                            if (key is not null) await mediaStorage.DeleteAsync(key, CancellationToken.None);
                        }
                        logger.LogWarning(ex, "Product media file skipped. BatchId={BatchId} ProductId={ProductId} SKU={SKU} Status=Failed", batch.Id, file.MatchedProductId, file.MatchedProductSku);
                    }
                }
                batch.Complete(clock.GetUtcNow(), imported, skipped, errors.Count);
            }, cancellationToken);
            await temporaryStorage.DeleteAsync(upload.Archive.ContainerId, CancellationToken.None);
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Product media batch completed. BatchId={BatchId} UserId={UserId} Status={Status}", batch.Id, upload.UserId, batch.Status);
            return new(batch.Id, batch.Status, imported, skipped, errors.Count, errors);
        }
        catch (Exception ex)
        {
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
