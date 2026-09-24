using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Ekiphan.Application.DataImport;

namespace Ekiphan.Infrastructure.DataImport;

public sealed partial class ProductImportMappingService : IProductImportMappingService
{
    private static readonly Dictionary<string, string> Aliases = BuildAliases();

    public string NormalizeHeader(string header)
    {
        ArgumentNullException.ThrowIfNull(header);
        var decomposed = MultipleSpaces().Replace(header.Trim(), " ")
            .ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var value in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(value) == UnicodeCategory.NonSpacingMark) continue;
            builder.Append(value switch { 'I' or 'İ' or 'ı' => 'I', _ => value });
        }
        return MultipleSpaces().Replace(builder.ToString().Normalize(NormalizationForm.FormC), " ");
    }

    public IReadOnlyList<ProductImportColumnMappingDto> Suggest(IReadOnlyList<string> headers) =>
        headers.Select(header => (Header: header, Normalized: NormalizeHeader(header)))
            .Where(x => Aliases.ContainsKey(x.Normalized))
            .Select(x => new ProductImportColumnMappingDto(x.Header, Aliases[x.Normalized]))
            .GroupBy(x => x.TargetField, StringComparer.Ordinal)
            .Select(x => x.First()).ToArray();

    private static Dictionary<string, string> BuildAliases()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        void Add(string target, params string[] aliases)
        {
            foreach (var alias in aliases) map[NormalizeStatic(alias)] = target;
        }
        Add(ProductImportField.ProductNameTr, "ÜRÜN ADI", "URUN ADI", "PRODUCT NAME");
        Add(ProductImportField.MainCategory, "ANA KATEGORİ", "ANA KATEGORI", "MAIN CATEGORY");
        Add(ProductImportField.Material, "MATERYAL", "MALZEME", "MATERIAL");
        Add(ProductImportField.LongDescriptionTr, "UZUN AÇIKLAMA", "UZUN ACIKLAMA");
        Add(ProductImportField.ShortDescriptionTr, "KISA AÇIKLAMA", "KISA ACIKLAMA");
        Add(ProductImportField.Brand, "MARKA", "BRAND");
        Add(ProductImportField.Sku, "STOK KODU", "SKU", "PRODUCT CODE", "ÜRÜN KODU", "URUN KODU");
        Add(ProductImportField.Color, "RENK", "COLOR");
        Add(ProductImportField.Size, "ÖLÇÜ", "OLCU", "SIZE");
        Add(ProductImportField.Tags, "ETİKET", "ETIKET", "ETİKETLER", "ETIKETLER", "TAGS");
        return map;
    }

    private static string NormalizeStatic(string value)
    {
        var decomposed = value.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        return new string(decomposed.Where(x => CharUnicodeInfo.GetUnicodeCategory(x) != UnicodeCategory.NonSpacingMark)
            .Select(x => x switch { 'I' or 'İ' or 'ı' => 'I', _ => x }).ToArray()).Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleSpaces();
}
