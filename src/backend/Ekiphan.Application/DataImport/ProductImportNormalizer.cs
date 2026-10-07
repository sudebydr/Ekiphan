using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Ekiphan.Domain.Common;

namespace Ekiphan.Application.DataImport;

public static class ProductImportNormalizer
{
    private static readonly IReadOnlyDictionary<string, string[]> Aliases = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["sku"] = ["sku", "urun kodu", "urun no", "stok kodu", "stok no"],
        ["name"] = ["urun adi", "urun adi uzun aciklama", "urun ismi", "ad"],
        ["brand"] = ["marka"],
        ["categories"] = ["kategori", "kategoriler", "ana kategori", "ana katagori"],
        ["subCategory1"] = ["1 alt kategori", "alt kategori 1"],
        ["subCategory2"] = ["2 alt kategori", "alt kategori 2"],
        ["subCategory3"] = ["3 alt kategori", "alt kategori 3"],
        ["tags"] = ["etiket", "etiketler", "etiket 1"],
        ["material"] = ["malzeme", "materyal"],
        ["shortDescription"] = ["kisa aciklama"],
        ["longDescription"] = ["uzun aciklama"],
        ["modelSeries"] = ["model seri", "model serisi"],
        ["packageQuantity"] = ["koli ici adet"],
        ["shape"] = ["sekil"]
    };

    private static readonly IReadOnlyDictionary<string, string[]> AttributeHeaders = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["PACKAGE_QUANTITY"] = ["koli ici adet"],
        ["COLOR"] = ["renk 1", "renk 2", "renk 3", "renk"],
        ["MODEL_SERIES"] = ["model seri", "model serisi"],
        ["USAGE_AREA"] = ["kullanim amaci kullanim yeri filtre icin 1", "kullanim amaci kullanim yeri filtre icin 2", "kullanim amaci kullanim yeri filtre icin 3", "kullanim alani", "kullanim amaci"],
        ["SHAPE"] = ["sekil"], ["VOLUME_CC"] = ["hacim olcu cc", "hacim cc"],
        ["VOLUME_LT"] = ["hacim olcu lt", "hacim lt"], ["DIAMETER_CM"] = ["cap cm o", "cap cm ø", "cap cm"],
        ["DIAMETER_MM"] = ["cap mm o", "cap mm ø", "cap mm"], ["WIDTH_CM"] = ["en cm"], ["WIDTH_MM"] = ["en mm"],
        ["LENGTH_CM"] = ["boy cm"], ["LENGTH_MM"] = ["boy mm"], ["HEIGHT_CM"] = ["yukseklik cm"],
        ["HEIGHT_MM"] = ["yukseklik mm"], ["WEIGHT_GR"] = ["urun agirlik gr", "agirlik gr"],
        ["MAIN_IMAGE_FILE"] = ["ana gorsel dosya adi"], ["ADDITIONAL_IMAGE_FILE"] = ["ilave gorsel dosya adi"],
        ["TECHNICAL_DRAWING_FILE"] = ["teknik cizim dokuman dosya adi"],
        ["TECHNICAL_FEATURE"] = ["teknik ozellik", "teknik ozellikler"],
        ["CATALOG_FILE_OR_URL"] = ["katalog dosyasi veya baglantisi"],
        ["SEARCH_SYNONYMS"] = ["arama es anlamlilari"], ["VARIANT"] = ["varyant", "varyant adi"],
        ["DECOR"] = ["dekor", "desen"], ["OPTION"] = ["secenek", "opsiyon"],
        ["SORT_PRIORITY"] = ["siralama oncelik"], ["PRICE_SEGMENT"] = ["fiyat segmenti"]
    };

    private static readonly HashSet<string> VariantAttributeCodes =
        ["COLOR", "MODEL_SERIES", "VOLUME_CC", "VOLUME_LT", "DIAMETER_CM", "DIAMETER_MM", "WIDTH_CM", "WIDTH_MM",
         "LENGTH_CM", "LENGTH_MM", "HEIGHT_CM", "HEIGHT_MM", "VARIANT", "DECOR", "OPTION"];
    private static readonly HashSet<string> KnownHeaders = BuildKnownHeaders();

    public static ProductImportNormalizationResult Normalize(TabularImportRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        var source = row.Values.ToDictionary(pair => NormalizeHeader(pair.Key), pair => Clean(pair.Value), StringComparer.Ordinal);
        var issues = source.Where(pair => !KnownHeaders.Contains(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => new ProductImportNormalizationIssue("UNKNOWN_COLUMN", pair.Key,
                "Column is not mapped; known columns in the row will still be imported.", true)).ToList();
        var skuValue = Find(source, "sku");
        var sku = string.IsNullOrWhiteSpace(skuValue) ? null : SkuNormalizer.Normalize(skuValue);
        var name = Find(source, "name");

        var categories = new[] { Find(source, "categories"), Find(source, "subCategory1"), Find(source, "subCategory2"), Find(source, "subCategory3") }
            .Where(value => !string.IsNullOrWhiteSpace(value)).Cast<string>().SelectMany(SplitMultiValue)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var tags = Enumerable.Range(1, 5).SelectMany(index => new[] { source.GetValueOrDefault($"etiket {index}"),
                source.GetValueOrDefault($"etiket arama motorlari icin {index}") })
            .Where(value => !string.IsNullOrWhiteSpace(value)).Cast<string>().Concat(SplitMultiValue(Find(source, "tags")))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var attributes = AttributeHeaders.ToDictionary(item => item.Key,
            item => item.Value.Select(source.GetValueOrDefault).Where(value => !string.IsNullOrWhiteSpace(value)).Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), StringComparer.Ordinal);
        attributes = attributes.Where(item => item.Value.Length > 0).ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        var variantValues = attributes.Where(item => VariantAttributeCodes.Contains(item.Key))
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        var variantKey = CreateVariantKey(variantValues);

        var values = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sku"] = sku, ["name"] = name, ["brand"] = Find(source, "brand"), ["material"] = Find(source, "material"),
            ["categories"] = categories, ["tags"] = tags, ["shortDescription"] = Find(source, "shortDescription"),
            ["longDescription"] = Find(source, "longDescription") ?? (source.ContainsKey("urun adi uzun aciklama") ? name : null),
            ["modelSeries"] = Find(source, "modelSeries"), ["packageQuantity"] = Find(source, "packageQuantity"),
            ["shape"] = Find(source, "shape"), ["attributeValues"] = attributes, ["variantValues"] = variantValues,
            ["variantKey"] = variantKey, ["metaTitle"] = source.GetValueOrDefault("seo basligi"),
            ["metaDescription"] = source.GetValueOrDefault("meta aciklamasi"), ["canonicalUrl"] = source.GetValueOrDefault("url adresi"),
            ["similarSkus"] = RelationSkus(source, "benzer urun kodu"),
            ["complementarySkus"] = RelationSkus(source, "tamamlayici urun kodu")
        };
        var isEffectivelyEmpty = IsEffectivelyEmptyProductRow(values);
        var hasProductIdentity = !string.IsNullOrWhiteSpace(sku) && !string.IsNullOrWhiteSpace(name);
        var shouldSkip = isEffectivelyEmpty || !hasProductIdentity;
        if (isEffectivelyEmpty)
        {
            issues.Add(new ProductImportNormalizationIssue(
                "EFFECTIVELY_EMPTY_PRODUCT_ROW",
                "row",
                "Row does not contain enough product data to create a product.",
                true));
        }
        else if (!hasProductIdentity)
        {
            issues.Add(new ProductImportNormalizationIssue(
                "INCOMPLETE_PRODUCT_ROW_SKIPPED",
                "row",
                "Row is missing SKU or product name and cannot create a product.",
                true));
        }

        return new ProductImportNormalizationResult(values, issues, shouldSkip);
    }

    public static string NormalizeHeader(string value)
    {
        var decomposed = value.Trim().Replace('\u0130', 'I').Replace('\u0131', 'i').ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.IsLetterOrDigit(character) ? character : ' ');
        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public static bool IsProductHeader(IReadOnlyCollection<string> headers)
    {
        var normalized = headers.Select(NormalizeHeader).ToHashSet(StringComparer.Ordinal);
        return Aliases["sku"].Any(normalized.Contains) && Aliases["name"].Any(normalized.Contains);
    }

    public static string VariantKeyHash(string variantKey) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(variantKey)))[..16];

    internal static string NormalizeValue(string value) => NormalizeHeader(value).ToUpperInvariant();
    private static bool IsEffectivelyEmptyProductRow(Dictionary<string, object?> values) =>
        HasProductContext(values) &&
        string.IsNullOrWhiteSpace(values["name"] as string) &&
        string.IsNullOrWhiteSpace(values["shortDescription"] as string) &&
        string.IsNullOrWhiteSpace(values["longDescription"] as string) &&
        string.IsNullOrWhiteSpace(values["material"] as string) &&
        string.IsNullOrWhiteSpace(values["modelSeries"] as string) &&
        string.IsNullOrWhiteSpace(values["packageQuantity"] as string) &&
        string.IsNullOrWhiteSpace(values["shape"] as string);

    private static bool HasProductContext(Dictionary<string, object?> values) =>
        !string.IsNullOrWhiteSpace(values["sku"] as string) ||
        !string.IsNullOrWhiteSpace(values["brand"] as string) ||
        ((string[])values["categories"]!).Length > 0 ||
        ((string[])values["tags"]!).Length > 0;
    private static string? CreateVariantKey(Dictionary<string, string[]> values) => values.Count == 0 ? null :
        string.Join('|', values.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item =>
            $"{item.Key}={string.Join(',', item.Value.Select(NormalizeValue).OrderBy(value => value, StringComparer.Ordinal))}"));
    private static string[] RelationSkus(Dictionary<string, string?> source, string prefix) => Enumerable.Range(1, 5)
        .Select(i => source.GetValueOrDefault($"{prefix} {i}")).Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => SkuNormalizer.Normalize(value!)).Distinct(StringComparer.Ordinal).ToArray();
    private static string? Find(Dictionary<string, string?> source, string canonicalName) => Aliases[canonicalName]
        .Select(alias => source.GetValueOrDefault(alias)).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    private static void AddRequiredIssue(List<ProductImportNormalizationIssue> issues, string? value, string column, string code)
    { if (string.IsNullOrWhiteSpace(value)) issues.Add(new(code, column, "Required import value is missing.")); }
    private static string[] SplitMultiValue(string? value) => string.IsNullOrWhiteSpace(value) ? [] : value
        .Split([';', '|'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static HashSet<string> BuildKnownHeaders()
    {
        var result = Aliases.Values.SelectMany(value => value).Concat(AttributeHeaders.Values.SelectMany(value => value))
            .Concat(["seo basligi", "meta aciklamasi", "url adresi"]).ToHashSet(StringComparer.Ordinal);
        for (var i = 1; i <= 5; i++) { result.Add($"etiket {i}"); result.Add($"etiket arama motorlari icin {i}"); result.Add($"benzer urun kodu {i}"); result.Add($"tamamlayici urun kodu {i}"); }
        return result;
    }
}

public sealed record ProductImportNormalizationResult(IReadOnlyDictionary<string, object?> Values,
    IReadOnlyList<ProductImportNormalizationIssue> Issues,
    bool ShouldSkip = false)
{
    public bool IsValid => Issues.All(issue => issue.IsWarning);
}

public sealed record ProductImportNormalizationIssue(string Code, string Column, string Message, bool IsWarning = false);
