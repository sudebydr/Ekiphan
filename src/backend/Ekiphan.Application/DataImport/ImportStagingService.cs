using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ekiphan.Domain.DataImport;

namespace Ekiphan.Application.DataImport;

public sealed class ImportStagingService(
    ITabularImportFileReader fileReader,
    IImportJobRepository repository,
    IImportReferenceResolver referenceResolver,
    TimeProvider timeProvider)
{
    private const int MaximumPayloadLength = 1_000_000;

    public async Task<ImportJob> StageAsync(
        StageImportFileCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Content);

        var options = command.ReadOptions ?? new ImportFileReadOptions();
        await using var content = await BufferAndValidateSizeAsync(
            command.Content,
            options.MaximumFileSizeBytes,
            cancellationToken);
        var checksum = Convert.ToHexString(SHA256.HashData(content));
        content.Position = 0;

        if (await repository.SourceExistsAsync(checksum, cancellationToken))
        {
            throw new DuplicateImportSourceException();
        }

        var sourceType = GetSourceType(command.FileName);
        var job = new ImportJob(
            Guid.NewGuid(),
            sourceType,
            command.FileName,
            checksum,
            command.IsDryRun,
            command.CreatedByUserId);
        repository.Add(job);
        var now = timeProvider.GetUtcNow();
        job.StartValidation(now);

        try
        {
            var document = await fileReader.ReadAsync(
                content,
                command.FileName,
                options,
                cancellationToken);

            foreach (var sheet in document.Sheets)
            {
                foreach (var sourceRow in sheet.Rows)
                {
                    StageRow(job, sheet.Name, sourceRow);
                }
            }

            MarkDuplicateSkus(job);
            await ResolveReferencesAsync(
                job,
                referenceResolver,
                cancellationToken);
            job.CompleteValidation(timeProvider.GetUtcNow());
        }
        catch (ImportFileReadException exception)
        {
            job.Fail(exception.Message, timeProvider.GetUtcNow());
        }

        await repository.SaveChangesAsync(cancellationToken);
        return job;
    }

    private static async Task ResolveReferencesAsync(
        ImportJob job,
        IImportReferenceResolver referenceResolver,
        CancellationToken cancellationToken)
    {
        var rows = job.Rows
            .Where(row => row.Status == ImportRowStatus.Valid)
            .ToArray();
        var payloads = rows.ToDictionary(
            row => row,
            row => JsonNode.Parse(row.NormalizedPayload!)!.AsObject());
        var brandNames = payloads.Values
            .Select(payload => payload["brand"]?.GetValue<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var categoryNames = payloads.Values
            .SelectMany(
                payload => payload["categories"]?.AsArray()
                    .Select(value => value?.GetValue<string>())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Cast<string>() ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var materialNames = payloads.Values
            .Select(payload => payload["material"]?.GetValue<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var tagNames = payloads.Values
            .SelectMany(
                payload => payload["tags"]?.AsArray()
                    .Select(value => value?.GetValue<string>())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Cast<string>() ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var resolution = await referenceResolver.ResolveAsync(
            brandNames,
            categoryNames,
            materialNames,
            tagNames,
            cancellationToken);

        foreach (var row in rows)
        {
            var payload = payloads[row];
            var hasError = false;
            var brandName = payload["brand"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(brandName))
            {
                if (resolution.BrandIds.TryGetValue(brandName, out var brandId))
                {
                    payload["brandId"] = brandId;
                }
                else
                {
                    AddReferenceIssue(
                        row,
                        "UNKNOWN_BRAND",
                        "Brand does not match an existing canonical brand.",
                        "brand",
                        brandName);
                    hasError = true;
                }
            }

            var categoryIds = new List<Guid>();
            foreach (var categoryName in payload["categories"]?.AsArray()
                .Select(value => value!.GetValue<string>()) ?? [])
            {
                if (resolution.AmbiguousCategoryNames.Contains(categoryName))
                {
                    AddReferenceIssue(
                        row,
                        "AMBIGUOUS_CATEGORY",
                        "Category name matches more than one Turkish category.",
                        "categories",
                        categoryName);
                    hasError = true;
                }
                else if (resolution.CategoryIds.TryGetValue(
                    categoryName,
                    out var categoryId))
                {
                    categoryIds.Add(categoryId);
                }
                else
                {
                    AddReferenceIssue(
                        row,
                        "UNKNOWN_CATEGORY",
                        "Category does not match an existing Turkish category.",
                        "categories",
                        categoryName);
                    hasError = true;
                }
            }

            var materialName = payload["material"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(materialName))
            {
                if (!resolution.MaterialAttributeId.HasValue)
                {
                    AddReferenceIssue(
                        row,
                        "MATERIAL_ATTRIBUTE_NOT_CONFIGURED",
                        "Active MATERIAL option attribute is not configured.",
                        "material",
                        materialName);
                    hasError = true;
                }
                else if (resolution.AmbiguousMaterialNames.Contains(materialName))
                {
                    AddReferenceIssue(
                        row,
                        "AMBIGUOUS_MATERIAL",
                        "Material matches more than one Turkish option.",
                        "material",
                        materialName);
                    hasError = true;
                }
                else if (resolution.MaterialOptionIds.TryGetValue(
                    materialName,
                    out var materialOptionId))
                {
                    payload["materialAttributeId"] =
                        resolution.MaterialAttributeId.Value;
                    payload["materialOptionId"] = materialOptionId;
                }
                else
                {
                    AddReferenceIssue(
                        row,
                        "UNKNOWN_MATERIAL",
                        "Material does not match an active Turkish option.",
                        "material",
                        materialName);
                    hasError = true;
                }
            }

            var tagIds = new List<Guid>();
            foreach (var tagName in payload["tags"]?.AsArray()
                .Select(value => value!.GetValue<string>()) ?? [])
            {
                if (resolution.AmbiguousTagNames.Contains(tagName))
                {
                    AddReferenceIssue(
                        row,
                        "AMBIGUOUS_TAG",
                        "Tag name matches more than one active Turkish tag.",
                        "tags",
                        tagName);
                    hasError = true;
                }
                else if (resolution.TagIds.TryGetValue(tagName, out var tagId))
                {
                    tagIds.Add(tagId);
                }
                else
                {
                    AddReferenceIssue(
                        row,
                        "UNKNOWN_TAG",
                        "Tag does not match an active Turkish tag.",
                        "tags",
                        tagName);
                    hasError = true;
                }
            }

            if (!hasError)
            {
                payload["categoryIds"] =
                    JsonSerializer.SerializeToNode(categoryIds);
                payload["tagIds"] = JsonSerializer.SerializeToNode(tagIds);
                row.MarkValid(payload.ToJsonString());
            }
        }
    }

    private static void AddReferenceIssue(
        ImportRow row,
        string code,
        string message,
        string columnName,
        string rawValue) =>
        row.AddIssue(
            Guid.NewGuid(),
            ImportIssueSeverity.Error,
            code,
            message,
            columnName,
            rawValue);

    private static void MarkDuplicateSkus(ImportJob job)
    {
        var duplicateRows = job.Rows
            .Where(row => !string.IsNullOrWhiteSpace(row.SKU))
            .GroupBy(row => row.SKU!, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group);

        foreach (var row in duplicateRows)
        {
            row.AddIssue(
                Guid.NewGuid(),
                ImportIssueSeverity.Error,
                "DUPLICATE_SKU_IN_SOURCE",
                "SKU occurs more than once in the same import source.",
                "sku",
                row.SKU);
        }
    }

    private static void StageRow(
        ImportJob job,
        string sheetName,
        TabularImportRow sourceRow)
    {
        var rawPayload = JsonSerializer.Serialize(sourceRow.Values);
        if (rawPayload.Length > MaximumPayloadLength)
        {
            throw new ImportFileReadException(
                $"Row {sourceRow.RowNumber} exceeds the staging payload limit.");
        }

        var normalization = ProductImportNormalizer.Normalize(sourceRow);
        var sku = normalization.Values["sku"] as string;
        var row = job.AddRow(
            Guid.NewGuid(),
            sheetName,
            sourceRow.RowNumber,
            rawPayload,
            sku);

        foreach (var issue in normalization.Issues)
        {
            row.AddIssue(
                Guid.NewGuid(),
                ImportIssueSeverity.Error,
                issue.Code,
                issue.Message,
                issue.Column);
        }

        if (normalization.IsValid)
        {
            row.MarkValid(JsonSerializer.Serialize(normalization.Values));
        }
    }

    private static ImportSourceType GetSourceType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".csv" => ImportSourceType.Csv,
            ".xlsx" => ImportSourceType.Excel,
            _ => throw new ImportFileReadException(
                "Only .csv and .xlsx import files are supported."),
        };

    private static async Task<MemoryStream> BufferAndValidateSizeAsync(
        Stream source,
        long maximumBytes,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        var destination = new MemoryStream();
        var buffer = new byte[81_920];
        long totalBytes = 0;

        while (true)
        {
            var count = await source.ReadAsync(
                buffer.AsMemory(),
                cancellationToken);
            if (count == 0)
            {
                break;
            }

            totalBytes += count;
            if (totalBytes > maximumBytes)
            {
                await destination.DisposeAsync();
                throw new ImportFileReadException(
                    $"File size exceeds the limit of {maximumBytes} bytes.");
            }

            await destination.WriteAsync(
                buffer.AsMemory(0, count),
                cancellationToken);
        }

        destination.Position = 0;
        return destination;
    }
}
