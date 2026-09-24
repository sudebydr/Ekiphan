using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Ekiphan.Application.DataImport;
using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.DataImport;
using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace Ekiphan.Infrastructure.DataImport;

internal sealed class ProductImportService(
    IEnumerable<IProductImportFileReader> readers,
    IProductImportMappingService mapping,
    IProductImportUploadStore store,
    IProductImportRepository repository,
    IValidator<ProductImportPreviewRequest> validator,
    IConfiguration configuration,
    TimeProvider timeProvider) : IProductImportService
{
    private readonly long _maximumBytes = configuration.GetValue(
        "ProductImport:MaximumFileSizeBytes", ImportFileReadOptions.DefaultMaximumFileSizeBytes);
    private readonly TimeSpan _tokenLifetime = TimeSpan.FromMinutes(
        configuration.GetValue("ProductImport:TokenLifetimeMinutes", 30));

    public async Task<ProductImportPreviewDto> PreviewAsync(ProductImportPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        if (request.Length > _maximumBytes) throw new ArgumentException($"File exceeds {_maximumBytes} bytes.");
        var reader = readers.SingleOrDefault(x => x.CanRead(request.FileName))
            ?? throw new ArgumentException("Unsupported file extension.");
        await using var buffer = new MemoryStream();
        await request.Content.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length == 0 || buffer.Length > _maximumBytes) throw new ArgumentException("File is empty or too large.");
        var hash = Convert.ToHexString(SHA256.HashData(buffer.ToArray()));
        buffer.Position = 0;
        var document = await reader.ReadAsync(buffer, Path.GetFileName(request.FileName),
            new ImportFileReadOptions { MaximumFileSizeBytes = _maximumBytes }, cancellationToken);
        var sheet = document.Sheets[0];
        var batch = new ImportBatch(Guid.NewGuid(), Path.GetFileName(request.FileName), hash, request.UserId);
        repository.AddBatch(batch);
        await repository.SaveChangesAsync(cancellationToken);
        var upload = new ProductImportStoredUpload(batch.Id, batch.FileName, request.ContentType, hash,
            document, timeProvider.GetUtcNow().Add(_tokenLifetime));
        var token = store.Put(upload);
        return new ProductImportPreviewDto(batch.Id, token, batch.FileName,
            document.Sheets.Sum(x => x.Rows.Count), sheet.Headers,
            sheet.Headers.Select(mapping.NormalizeHeader).ToArray(), mapping.Suggest(sheet.Headers),
            document.Sheets.SelectMany(x => x.Rows).Take(20).Select(x => x.Values).ToArray());
    }

    public async Task<ProductImportBatchDetailDto?> GetBatchAsync(Guid batchId,
        CancellationToken cancellationToken = default)
    {
        var batch = await repository.GetBatchAsync(batchId, false, cancellationToken);
        if (batch is null) return null;
        return new ProductImportBatchDetailDto(batch.Id, batch.FileName, batch.Status, batch.TotalRows,
            batch.Items.Count(x => x.Status != ImportBatchItemStatus.Invalid), batch.ErrorCount,
            batch.SuccessCount, batch.Items.Count(x => x.Status == ImportBatchItemStatus.Imported),
            batch.FailureReason, batch.CreatedAt, batch.CompletedAt, batch.RolledBackAt, batch.RollbackSummary);
    }

    public async Task WriteErrorsCsvAsync(Guid batchId, Stream destination,
        CancellationToken cancellationToken = default)
    {
        var batch = await repository.GetBatchAsync(batchId, false, cancellationToken)
            ?? throw new KeyNotFoundException("Import batch was not found.");
        await using var writer = new StreamWriter(destination, new UTF8Encoding(true), leaveOpen: true);
        await writer.WriteLineAsync("RowNumber,SKU,Field,ErrorCode,ErrorMessage");
        foreach (var item in batch.Items.Where(x => x.Status == ImportBatchItemStatus.Invalid))
        {
            static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
            await writer.WriteLineAsync($"{item.RowNumber},{Csv(item.Sku)},{Csv(item.ErrorField)},{Csv(item.ErrorCode)},{Csv(item.ErrorMessage)}");
        }
        await writer.FlushAsync(cancellationToken);
    }
}

internal sealed class ProductImportValidationService(
    IProductImportUploadStore store,
    IProductImportRepository repository,
    IValidator<ProductImportValidateCommand> validator,
    TimeProvider timeProvider) : IProductImportValidationService
{
    public async Task<ProductImportValidationResultDto> ValidateAsync(ProductImportValidateCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        if (!store.TryGet(command.UploadToken, out var upload))
            throw new ProductImportTokenException("Upload token is invalid or expired.");
        var batch = await repository.GetBatchAsync(upload.BatchId, true, cancellationToken)
            ?? throw new KeyNotFoundException("Import batch was not found.");
        batch.StartValidation(timeProvider.GetUtcNow());
        var allHeaders = upload.Document.Sheets.SelectMany(x => x.Headers).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (command.Mappings.Any(x => !allHeaders.Contains(x.SourceHeader)))
            throw new ArgumentException("A mapped source header does not exist in the upload.");
        var map = command.Mappings.ToDictionary(x => x.TargetField, x => x.SourceHeader, StringComparer.Ordinal);
        var rows = upload.Document.Sheets.SelectMany(x => x.Rows).ToArray();
        string? Value(TabularImportRow row, string field) => map.TryGetValue(field, out var header) &&
            row.Values.TryGetValue(header, out var value) ? value?.Trim() : null;
        var candidates = rows.Select(row => new ProductImportNormalizedRow(row.RowNumber,
            Value(row, ProductImportField.Sku)?.ToUpperInvariant(), Value(row, ProductImportField.ProductNameTr),
            Value(row, ProductImportField.MainCategory), Value(row, ProductImportField.Material),
            Value(row, ProductImportField.LongDescriptionTr), Value(row, ProductImportField.ShortDescriptionTr),
            Value(row, ProductImportField.Brand), Value(row, ProductImportField.Color), Value(row, ProductImportField.Size),
            Split(Value(row, ProductImportField.Tags)), null, null)).ToArray();
        var duplicateSkus = candidates.Where(x => !string.IsNullOrWhiteSpace(x.Sku)).GroupBy(x => x.Sku!, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1).Select(x => x.Key).Order().ToArray();
        var existing = await repository.GetExistingSkusAsync(candidates.Select(x => x.Sku).Where(x => x is not null)!, cancellationToken);
        var brands = await repository.GetBrandsAsync(candidates.Select(x => x.Brand).Where(x => x is not null)!, cancellationToken);
        var categories = await repository.GetCategoriesAsync(candidates.Select(x => x.MainCategory).Where(x => x is not null)!, cancellationToken);
        var errors = new List<ProductImportRowErrorDto>();
        var valid = new List<ProductImportNormalizedRow>();
        foreach (var row in candidates)
        {
            void Error(string field, string code, string message) => errors.Add(new(row.RowNumber, row.Sku, field, code, message));
            if (string.IsNullOrWhiteSpace(row.Sku)) Error(ProductImportField.Sku, "SKU_REQUIRED", "SKU zorunludur.");
            else if (row.Sku.Length > 100) Error(ProductImportField.Sku, "SKU_TOO_LONG", "SKU 100 karakteri geçemez.");
            else if (duplicateSkus.Contains(row.Sku, StringComparer.OrdinalIgnoreCase)) Error(ProductImportField.Sku, "DUPLICATE_SKU", "Dosyada mükerrer SKU.");
            else if (command.ImportOptions.RejectExistingSkus && existing.Contains(row.Sku)) Error(ProductImportField.Sku, "SKU_EXISTS", "SKU veritabanında mevcut.");
            if (string.IsNullOrWhiteSpace(row.ProductNameTr)) Error(ProductImportField.ProductNameTr, "PRODUCT_NAME_REQUIRED", "Türkçe ürün adı zorunludur.");
            else if (row.ProductNameTr.Length > 250) Error(ProductImportField.ProductNameTr, "PRODUCT_NAME_TOO_LONG", "Ürün adı 250 karakteri geçemez.");
            if (row.ShortDescriptionTr?.Length > 500) Error(ProductImportField.ShortDescriptionTr, "VALUE_TOO_LONG", "Kısa açıklama 500 karakteri geçemez.");
            if (row.LongDescriptionTr?.Length > 20000) Error(ProductImportField.LongDescriptionTr, "VALUE_TOO_LONG", "Uzun açıklama çok uzun.");
            if (!string.IsNullOrWhiteSpace(row.Brand) && !brands.ContainsKey(row.Brand)) Error(ProductImportField.Brand, "BRAND_NOT_FOUND", "Marka bulunamadı.");
            if (!string.IsNullOrWhiteSpace(row.MainCategory) && !categories.ContainsKey(row.MainCategory)) Error(ProductImportField.MainCategory, "CATEGORY_NOT_FOUND", "Ana kategori bulunamadı.");
            if (errors.All(x => x.RowNumber != row.RowNumber)) valid.Add(row with
            {
                BrandId = row.Brand is not null && brands.TryGetValue(row.Brand, out var brandId) ? brandId : null,
                CategoryId = row.MainCategory is not null && categories.TryGetValue(row.MainCategory, out var categoryId) ? categoryId : null,
            });
        }
        var items = candidates.Select(row =>
        {
            var error = errors.FirstOrDefault(x => x.RowNumber == row.RowNumber);
            return new ImportBatchItem(Guid.NewGuid(), batch.Id, row.RowNumber, row.Sku,
                error is null ? ImportBatchItemStatus.Valid : ImportBatchItemStatus.Invalid,
                error?.Code, error?.Message, error?.Field);
        }).ToArray();
        batch.SetValidation(items, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        var validationToken = store.PutValidation(new ProductImportStoredValidation(batch.Id, upload.Sha256,
            valid, errors, command.ImportOptions, timeProvider.GetUtcNow().AddMinutes(30)));
        var invalidRows = errors.Select(x => x.RowNumber).Distinct().Count();
        return new ProductImportValidationResultDto(batch.Id, candidates.Length, candidates.Length - invalidRows,
            invalidRows, duplicateSkus, existing.Order().ToArray(),
            candidates.Select(x => x.Brand).Where(x => x is not null && !brands.ContainsKey(x)).Distinct(StringComparer.OrdinalIgnoreCase).Cast<string>().ToArray(),
            candidates.Select(x => x.MainCategory).Where(x => x is not null && !categories.ContainsKey(x)).Distinct(StringComparer.OrdinalIgnoreCase).Cast<string>().ToArray(),
            errors.Where(x => x.Code.EndsWith("_REQUIRED", StringComparison.Ordinal)).Select(x => x.Field).Distinct().ToArray(), errors, validationToken);
    }

    private static string[] Split(string? value) => string.IsNullOrWhiteSpace(value) ? [] :
        value.Split([';', '|', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
}

internal sealed class ProductImportExecutionService(
    IProductImportUploadStore store,
    IProductImportRepository repository,
    IValidator<ProductImportExecuteCommand> validator,
    TimeProvider timeProvider) : IProductImportExecutionService
{
    public async Task<ProductImportExecutionResultDto> ExecuteAsync(ProductImportExecuteCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        if (!store.TryGetValidation(command.ValidationToken, out var validation))
            throw new ProductImportTokenException("Validation token is invalid or expired.");
        if (validation.Errors.Count > 0 && !validation.Options.SkipInvalidRows)
            throw new ProductImportConflictException("Dry-run contains errors. Fix them or enable SkipInvalidRows.");
        try
        {
            ProductImportExecutionResultDto? result = null;
            await repository.ExecuteTransactionAsync(async ct =>
            {
                var batch = await repository.GetBatchAsync(validation.BatchId, true, ct)
                    ?? throw new KeyNotFoundException("Import batch was not found.");
                if (!string.Equals(batch.OriginalFileHash, validation.FileHash, StringComparison.Ordinal))
                    throw new ProductImportConflictException("The upload changed after dry-run validation.");
                batch.StartProcessing(timeProvider.GetUtcNow());
                var pairs = new List<(Product Product, ImportBatchItem Item)>();
                foreach (var row in validation.ValidRows)
                {
                    var product = new Product(Guid.NewGuid(), row.Sku!, row.BrandId);
                    product.AddTranslation("tr", row.ProductNameTr!, Slug(row.ProductNameTr!, product.Id),
                        row.ShortDescriptionTr, row.LongDescriptionTr);
                    if (row.CategoryId.HasValue) product.AddCategory(row.CategoryId.Value, true);
                    product.MarkImported(batch.Id);
                    repository.AddProduct(product);
                    var item = batch.Items.Single(x => x.RowNumber == row.RowNumber);
                    pairs.Add((product, item));
                }
                await repository.SaveChangesAsync(ct);
                foreach (var pair in pairs) pair.Item.MarkImported(pair.Product.Id, pair.Product.UpdatedAt);
                batch.CompleteProcessing(validation.Errors.Count > 0, timeProvider.GetUtcNow());
                await repository.SaveChangesAsync(ct);
                result = new ProductImportExecutionResultDto(batch.Id, batch.Status, pairs.Count, validation.Errors.Select(x => x.RowNumber).Distinct().Count());
            }, cancellationToken);
            return result!;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var batch = await repository.GetBatchAsync(validation.BatchId, true, cancellationToken);
            if (batch is not null && batch.Status != ImportBatchStatus.Failed)
            {
                batch.Fail(exception.Message, timeProvider.GetUtcNow());
                await repository.SaveChangesAsync(cancellationToken);
            }
            throw;
        }
    }

    private static string Slug(string name, Guid id)
    {
        var chars = name.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD)
            .Where(x => CharUnicodeInfo.GetUnicodeCategory(x) != UnicodeCategory.NonSpacingMark)
            .Select(x => char.IsLetterOrDigit(x) ? x : '-').ToArray();
        var value = string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
        return $"{(string.IsNullOrEmpty(value) ? "product" : value)}-{id:N}";
    }
}

internal sealed class ProductImportRollbackService(
    IProductImportRepository repository,
    IValidator<ProductImportRollbackCommand> validator,
    TimeProvider timeProvider) : IProductImportRollbackService
{
    public async Task<ProductImportRollbackResultDto> RollbackAsync(ProductImportRollbackCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        ProductImportRollbackResultDto? result = null;
        await repository.ExecuteTransactionAsync(async ct =>
        {
            var batch = await repository.GetBatchAsync(command.BatchId, true, ct)
                ?? throw new KeyNotFoundException("Import batch was not found.");
            var rolledBack = 0;
            var skipped = 0;
            foreach (var item in batch.Items.Where(x => x.Status == ImportBatchItemStatus.Imported))
            {
                var product = item.ProductId.HasValue ? await repository.GetProductAsync(item.ProductId.Value, ct) : null;
                var changed = product is null || product.ImportedByBatchId != batch.Id ||
                    product.UpdatedAt != item.ImportedProductVersion;
                if (changed) { item.MarkRollback(true); skipped++; continue; }
                product!.SoftDelete(timeProvider.GetUtcNow());
                item.MarkRollback(false);
                rolledBack++;
            }
            batch.CompleteRollback(command.UserId, rolledBack, skipped, timeProvider.GetUtcNow());
            await repository.SaveChangesAsync(ct);
            result = new ProductImportRollbackResultDto(batch.Id, rolledBack, skipped, batch.Status);
        }, cancellationToken);
        return result!;
    }
}
