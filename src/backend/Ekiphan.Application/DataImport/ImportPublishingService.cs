using System.Globalization;
using System.Text;
using System.Text.Json;
using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.DataImport;

namespace Ekiphan.Application.DataImport;

public sealed class ImportPublishingService(
    IImportPublishingRepository repository,
    TimeProvider timeProvider)
{
    private const int BatchSize = 500;

    public async Task<ImportPublishResult> PublishAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        ImportPublishResult? result = null;

        await repository.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                var job = await repository.GetJobAsync(
                    jobId,
                    transactionCancellationToken) ??
                    throw new ImportJobNotFoundException(jobId);
                job.StartPublishing(timeProvider.GetUtcNow());
                var validRows = job.Rows
                    .Where(row => row.Status == ImportRowStatus.Valid)
                    .ToArray();
                var existingSkus = await repository.GetExistingSkusAsync(
                    validRows.Select(row => row.SKU!).ToArray(),
                    transactionCancellationToken);
                var pendingChanges = 0;

                foreach (var row in validRows)
                {
                    if (existingSkus.Contains(row.SKU!))
                    {
                        row.MarkSkipped();
                    }
                    else
                    {
                        var product = CreateProduct(row);
                        repository.AddProduct(product);
                        var materialValue = CreateMaterialValue(row, product.Id);
                        if (materialValue is not null)
                        {
                            repository.AddAttributeValue(materialValue);
                        }

                        existingSkus.Add(product.SKU);
                        row.MarkPublished();
                    }

                    pendingChanges++;
                    if (pendingChanges == BatchSize)
                    {
                        await repository.SaveChangesAsync(
                            transactionCancellationToken);
                        pendingChanges = 0;
                    }
                }

                job.CompletePublishing(timeProvider.GetUtcNow());
                await repository.SaveChangesAsync(transactionCancellationToken);
                result = new ImportPublishResult(
                    job.Id,
                    job.Status,
                    job.PublishedRowCount,
                    job.Rows.Count(row => row.Status == ImportRowStatus.Skipped));
            },
            cancellationToken);

        return result!;
    }

    private static Product CreateProduct(ImportRow row)
    {
        using var payload = JsonDocument.Parse(row.NormalizedPayload!);
        var root = payload.RootElement;
        var sku = root.GetProperty("sku").GetString()!;
        var name = root.GetProperty("name").GetString()!;
        var brandId = root.TryGetProperty("brandId", out var brandElement) &&
            brandElement.ValueKind == JsonValueKind.String
            ? brandElement.GetGuid()
            : (Guid?)null;
        var product = new Product(Guid.NewGuid(), sku, brandId);
        product.AddTranslation(
            "tr",
            name,
            CreateSlug(name, product.Id));
        if (root.TryGetProperty("categoryIds", out var categoryElements))
        {
            foreach (var categoryId in categoryElements
                .EnumerateArray()
                .Select(element => element.GetGuid())
                .Distinct())
            {
                product.AddCategory(categoryId);
            }
        }

        if (root.TryGetProperty("tagIds", out var tagElements))
        {
            var sortOrder = 0;
            foreach (var tagId in tagElements
                .EnumerateArray()
                .Select(element => element.GetGuid())
                .Distinct())
            {
                product.AddTag(tagId, sortOrder++);
            }
        }

        return product;
    }

    private static ProductAttributeValue? CreateMaterialValue(
        ImportRow row,
        Guid productId)
    {
        using var payload = JsonDocument.Parse(row.NormalizedPayload!);
        var root = payload.RootElement;
        if (!root.TryGetProperty(
                "materialAttributeId",
                out var attributeElement) ||
            !root.TryGetProperty("materialOptionId", out var optionElement))
        {
            return null;
        }

        var rawValue = root.TryGetProperty("material", out var materialElement)
            ? materialElement.GetString()
            : null;
        return ProductAttributeValue.FromOption(
            Guid.NewGuid(),
            productId,
            attributeElement.GetGuid(),
            optionElement.GetGuid(),
            rawValue: rawValue);
    }

    private static string CreateSlug(string name, Guid productId)
    {
        var decomposed = name.Trim().ToLowerInvariant()
            .Replace('ı', 'i')
            .Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var previousWasSeparator = false;

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator && builder.Length > 0)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        var namePart = builder.ToString().Trim('-');
        if (namePart.Length > 260)
        {
            namePart = namePart[..260].TrimEnd('-');
        }

        return $"{(namePart.Length == 0 ? "product" : namePart)}-{productId:N}";
    }
}
