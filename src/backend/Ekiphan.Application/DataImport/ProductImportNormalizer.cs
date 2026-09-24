using System.Globalization;
using System.Text;

namespace Ekiphan.Application.DataImport;

public static class ProductImportNormalizer
{
    private static readonly Dictionary<string, string[]> Aliases =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["sku"] = ["sku", "urun kodu", "urun no", "stok kodu"],
            ["name"] = ["urun adi", "urun ismi", "ad"],
            ["brand"] = ["marka"],
            ["categories"] = ["kategori", "kategoriler"],
            ["tags"] = ["etiket", "etiketler"],
            ["material"] = ["malzeme"]
        };

    public static ProductImportNormalizationResult Normalize(TabularImportRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        var source = row.Values.ToDictionary(
            pair => NormalizeHeader(pair.Key),
            pair => Clean(pair.Value),
            StringComparer.Ordinal);
        var issues = new List<ProductImportNormalizationIssue>();
        var sku = Find(source, "sku")?.ToUpperInvariant();
        var name = Find(source, "name");

        AddRequiredIssue(issues, sku, "sku", "SKU_REQUIRED");
        AddRequiredIssue(issues, name, "name", "PRODUCT_NAME_REQUIRED");

        var values = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sku"] = sku,
            ["name"] = name,
            ["brand"] = Find(source, "brand"),
            ["material"] = Find(source, "material"),
            ["categories"] = SplitMultiValue(Find(source, "categories")),
            ["tags"] = SplitMultiValue(Find(source, "tags"))
        };

        return new ProductImportNormalizationResult(values, issues);
    }

    private static string? Find(
        Dictionary<string, string?> source,
        string canonicalName)
    {
        foreach (var alias in Aliases[canonicalName])
        {
            if (source.TryGetValue(alias, out var value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static void AddRequiredIssue(
        List<ProductImportNormalizationIssue> issues,
        string? value,
        string column,
        string code)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add(
                new ProductImportNormalizationIssue(
                    code,
                    column,
                    "Required import value is missing."));
        }
    }

    private static string[] SplitMultiValue(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split([';', '|'], StringSplitOptions.TrimEntries |
                StringSplitOptions.RemoveEmptyEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeHeader(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant()
            .Replace('ı', 'i')
            .Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) !=
                UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.IsLetterOrDigit(character) ? character : ' ');
            }
        }

        return string.Join(
            ' ',
            builder.ToString().Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries));
    }
}

public sealed record ProductImportNormalizationResult(
    IReadOnlyDictionary<string, object?> Values,
    IReadOnlyList<ProductImportNormalizationIssue> Issues)
{
    public bool IsValid => Issues.Count == 0;
}

public sealed record ProductImportNormalizationIssue(
    string Code,
    string Column,
    string Message);
