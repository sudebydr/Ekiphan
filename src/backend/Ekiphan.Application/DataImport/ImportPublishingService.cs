using System.Globalization;
using System.Text;
using System.Text.Json;
using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.Common;
using Ekiphan.Domain.DataImport;

namespace Ekiphan.Application.DataImport;

public sealed class ImportPublishingService(
    IImportPublishingRepository repository,
    TimeProvider timeProvider)
{
    private const int BatchSize = 500;
    private const int MetaTitleMaximumLength = 70;
    private static readonly (string Property, string Label)[] ProductLevelProperties =
    [
        ("name", "name"), ("brandId", "brand"), ("categoryIds", "category"), ("tagIds", "tags"),
        ("shortDescription", "shortDescription"), ("longDescription", "longDescription"),
        ("metaTitle", "metaTitle"), ("metaDescription", "metaDescription"), ("canonicalUrl", "canonicalUrl"),
        ("material", "material"), ("materialAttributeId", "material"), ("materialOptionId", "material")
    ];

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
                var existingProducts = await repository.GetProductsBySkusAsync(
                    validRows.Select(row => SkuNormalizer.Normalize(row.SKU!)).ToArray(),
                    transactionCancellationToken);
                var groups = new List<(ImportRow[] Rows, Product Product)>();

                foreach (var rowGroup in validRows.GroupBy(row => SkuNormalizer.Normalize(row.SKU!), StringComparer.Ordinal))
                {
                    var rows = rowGroup.OrderBy(row => row.RowNumber).ToArray();
                    var row = rows[0];
                    AddProductLevelDifferenceWarnings(row, rows.Skip(1));
                    Product product;
                    if (existingProducts.TryGetValue(rowGroup.Key, out var existingProduct))
                    {
                        UpdateProduct(existingProduct, row);
                        product = existingProduct;
                    }
                    else
                    {
                        product = CreateProduct(row);
                        product.SetImportCreationOwner(job.Id);
                        repository.AddProduct(product);
                        existingProducts = new Dictionary<string, Product>(existingProducts, StringComparer.OrdinalIgnoreCase)
                        {
                            [SkuNormalizer.Normalize(product.SKU)] = product
                        };
                    }

                    foreach (var publishedRow in rows)
                        publishedRow.MarkPublished();
                    groups.Add((rows, product));
                }

                var attributeCodes = groups.SelectMany(group => group.Rows)
                    .SelectMany(ReadAttributes)
                    .Select(item => item.Key)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                var materialAttributeIds = groups.Select(group => ReadMaterial(group.Rows[0]).AttributeId)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .Distinct()
                    .ToArray();
                await repository.PrepareProductDataAsync(
                    groups.Select(group => group.Product.Id).Distinct().ToArray(),
                    attributeCodes,
                    materialAttributeIds,
                    transactionCancellationToken);

                var pendingChanges = 0;
                foreach (var group in groups)
                {
                    var rows = group.Rows;
                    var product = group.Product;
                    var attributes = rows.Select(ReadAttributes).SelectMany(item => item)
                        .GroupBy(item => item.Key, StringComparer.Ordinal)
                        .ToDictionary(group => group.Key,
                            group => group.SelectMany(item => item.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                            StringComparer.Ordinal);
                    await repository.ReplaceImportedAttributesAsync(product.Id, attributes,
                        transactionCancellationToken);
                    var material = ReadMaterial(rows[0]);
                    await repository.ReplaceMaterialAsync(product.Id, material.AttributeId, material.OptionId,
                        material.RawValue, transactionCancellationToken);
                    var variantOrder = 0;
                    foreach (var variantKey in rows.Select(ReadVariantKey).Where(value => !string.IsNullOrWhiteSpace(value))
                        .Cast<string>().Distinct(StringComparer.Ordinal))
                    {
                        await repository.UpsertVariantAsync(product, variantKey, variantOrder++, transactionCancellationToken);
                    }

                    pendingChanges += rows.Length;
                    if (pendingChanges >= BatchSize)
                    {
                        await repository.SaveChangesAsync(
                            transactionCancellationToken);
                        pendingChanges = 0;
                    }
                }

                await repository.SaveChangesAsync(transactionCancellationToken);
                var relationRequests = validRows.Select(row =>
                {
                    var product = existingProducts[SkuNormalizer.Normalize(row.SKU!)];
                    var (similar, complementary) = ReadRelations(row);
                    return new ImportRelationRequest(row.Id, product.Id, similar, complementary);
                }).ToArray();
                await repository.ApplyRelationsBatchAsync(relationRequests, transactionCancellationToken);

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
        var sku = SkuNormalizer.Normalize(root.GetProperty("sku").GetString()!);
        var name = root.GetProperty("name").GetString()!;
        var brandId = root.TryGetProperty("brandId", out var brandElement) &&
            brandElement.ValueKind == JsonValueKind.String
            ? brandElement.GetGuid()
            : (Guid?)null;
        var product = new Product(Guid.NewGuid(), sku, brandId);
        product.SetPublished(true);
        product.AddTranslation(
            "tr",
            name,
            CreateSlug(name, product.Id),
            GetString(root, "shortDescription"),
            GetString(root, "longDescription"), LimitOptionalString(GetString(root, "metaTitle"), MetaTitleMaximumLength),
            GetString(root, "metaDescription"), GetString(root, "canonicalUrl"));
        if (root.TryGetProperty("categoryIds", out var categoryElements))
        {
            foreach (var categoryId in categoryElements
                .EnumerateArray()
                .Select(element => element.GetGuid())
                .Distinct())
            {
                product.AddCategory(categoryId, isPrimary: product.Categories.Count == 0,
                    sortOrder: product.Categories.Count);
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

    private static void UpdateProduct(Product product, ImportRow row)
    {
        using var payload = JsonDocument.Parse(row.NormalizedPayload!);
        var root = payload.RootElement;
        var name = root.GetProperty("name").GetString()!;
        var brandId = root.TryGetProperty("brandId", out var brand) && brand.ValueKind == JsonValueKind.String
            ? brand.GetGuid() : (Guid?)null;
        product.UpdateIdentity(product.SKU, brandId);
        var translation = product.Translations.SingleOrDefault(item => item.LanguageCode == "tr");
        product.SetTranslation("tr", name, translation?.Slug ?? CreateSlug(name, product.Id),
            GetString(root, "shortDescription"), GetString(root, "longDescription"),
            LimitOptionalString(GetString(root, "metaTitle"), MetaTitleMaximumLength), GetString(root, "metaDescription"), GetString(root, "canonicalUrl"));
        var categoryIds = root.TryGetProperty("categoryIds", out var categories)
            ? categories.EnumerateArray().Select(item => item.GetGuid()).Distinct().ToArray() : [];
        product.SetCategories(categoryIds, categoryIds.FirstOrDefault() is var primary && primary != Guid.Empty ? primary : null);
        var tagIds = root.TryGetProperty("tagIds", out var tags)
            ? tags.EnumerateArray().Select(item => item.GetGuid()).Distinct().ToArray() : [];
        product.SetTags(tagIds);
    }

    private static string? GetString(JsonElement root, string property) =>
        root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() : null;

    private static string? LimitOptionalString(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
    }

    private void AddProductLevelDifferenceWarnings(ImportRow canonicalRow, IEnumerable<ImportRow> rows)
    {
        using var canonicalPayload = JsonDocument.Parse(canonicalRow.NormalizedPayload!);
        foreach (var row in rows)
        {
            using var payload = JsonDocument.Parse(row.NormalizedPayload!);
            var differentFields = ProductLevelProperties
                .Where(property => !JsonPropertiesMatch(canonicalPayload.RootElement, payload.RootElement, property.Property))
                .Select(property => property.Label)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (differentFields.Length == 0) continue;

            row.AddIssue(Guid.NewGuid(), ImportIssueSeverity.Warning, "SAME_SKU_PRODUCT_DATA_DIFFER",
                $"SKU '{canonicalRow.SKU}' canonical row {canonicalRow.RowNumber} differs from row {row.RowNumber} in product-level fields: {string.Join(", ", differentFields)}.",
                "product", canonicalRow.SKU);
            repository.AddIssue(row.Issues.Last());
        }
    }

    private static bool JsonPropertiesMatch(JsonElement left, JsonElement right, string property)
    {
        var hasLeft = left.TryGetProperty(property, out var leftValue);
        var hasRight = right.TryGetProperty(property, out var rightValue);
        return hasLeft == hasRight && (!hasLeft || leftValue.GetRawText() == rightValue.GetRawText());
    }

    private static Dictionary<string, string[]> ReadAttributes(ImportRow row)
    {
        using var payload = JsonDocument.Parse(row.NormalizedPayload!);
        if (!payload.RootElement.TryGetProperty("attributeValues", out var values))
            return new Dictionary<string, string[]>();
        return values.EnumerateObject().ToDictionary(item => item.Name,
            item => item.Value.EnumerateArray().Select(value => value.GetString()!).ToArray(), StringComparer.Ordinal);
    }

    private static (string[] Similar, string[] Complementary) ReadRelations(ImportRow row)
    {
        using var payload = JsonDocument.Parse(row.NormalizedPayload!);
        string[] Read(string property) => payload.RootElement.TryGetProperty(property, out var values)
            ? values.EnumerateArray().Select(value => value.GetString()!).ToArray() : [];
        return (Read("similarSkus"), Read("complementarySkus"));
    }

    private static (Guid? AttributeId, Guid? OptionId, string? RawValue) ReadMaterial(ImportRow row)
    {
        using var payload = JsonDocument.Parse(row.NormalizedPayload!);
        var root = payload.RootElement;
        if (!root.TryGetProperty(
                "materialAttributeId",
                out var attributeElement) ||
            !root.TryGetProperty("materialOptionId", out var optionElement))
        {
            return (null, null, null);
        }

        var rawValue = root.TryGetProperty("material", out var materialElement)
            ? materialElement.GetString()
            : null;
        return (attributeElement.GetGuid(), optionElement.GetGuid(), rawValue);
    }

    private static string? ReadVariantKey(ImportRow row)
    {
        using var payload = JsonDocument.Parse(row.NormalizedPayload!);
        return GetString(payload.RootElement, "variantKey");
    }

    private static string CreateSlug(string name, Guid productId)
    {
        var decomposed = name.Trim()
            .Replace('İ', 'I')
            .Replace('ı', 'i')
            .ToLowerInvariant()
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

            foreach (var mapped in ToAscii(character))
            {
                if (mapped is >= 'a' and <= 'z' or >= '0' and <= '9')
                {
                    builder.Append(mapped);
                    previousWasSeparator = false;
                }
                else if (!previousWasSeparator && builder.Length > 0)
                {
                    builder.Append('-');
                    previousWasSeparator = true;
                }
            }
        }

        var namePart = builder.ToString().Trim('-');
        if (namePart.Length > 260)
        {
            namePart = namePart[..260].TrimEnd('-');
        }

        return $"{(namePart.Length == 0 ? "product" : namePart)}-{productId:N}";
    }

    private static string ToAscii(char character) =>
        character switch
        {
            'ø' => "o",
            'ß' => "ss",
            'æ' => "ae",
            'œ' => "oe",
            'đ' => "d",
            'ł' => "l",
            'þ' => "th",
            _ => character.ToString(),
        };
}
