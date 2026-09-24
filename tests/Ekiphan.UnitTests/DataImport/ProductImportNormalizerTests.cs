using Ekiphan.Application.DataImport;

namespace Ekiphan.UnitTests.DataImport;

public sealed class ProductImportNormalizerTests
{
    [Fact]
    public void NormalizeMapsTurkishHeadersAndMultiValues()
    {
        var row = CreateRow(
            new Dictionary<string, string?>
            {
                ["Ürün Kodu"] = "  ab-123 ",
                ["Ürün Adı"] = " Servis Tabağı ",
                ["Marka"] = "Ekiphan",
                ["Kategoriler"] = "Servis; Sunum | Servis",
                ["Etiketler"] = "otel; premium"
            });

        var result = ProductImportNormalizer.Normalize(row);

        Assert.True(result.IsValid);
        Assert.Equal("AB-123", result.Values["sku"]);
        Assert.Equal("Servis Tabağı", result.Values["name"]);
        Assert.Equal(
            ["Servis", "Sunum"],
            Assert.IsType<string[]>(result.Values["categories"]));
    }

    [Fact]
    public void NormalizeReportsMissingRequiredValues()
    {
        var result = ProductImportNormalizer.Normalize(
            CreateRow(
                new Dictionary<string, string?>
                {
                    ["Marka"] = "Ekiphan"
                }));

        Assert.False(result.IsValid);
        Assert.Equal(
            ["SKU_REQUIRED", "PRODUCT_NAME_REQUIRED"],
            result.Issues.Select(issue => issue.Code));
    }

    private static TabularImportRow CreateRow(
        IReadOnlyDictionary<string, string?> values) =>
        new(2, values);
}
