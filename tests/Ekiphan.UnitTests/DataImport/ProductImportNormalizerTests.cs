using Ekiphan.Application.DataImport;
using Ekiphan.Domain.Common;

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
    public void NormalizeSkipsRowWhenSkuIsMissing()
    {
        var result = ProductImportNormalizer.Normalize(
            CreateRow(
                new Dictionary<string, string?>
                {
                    ["Urun Adi"] = "Product"
                }));

        Assert.True(result.IsValid);
        Assert.True(result.ShouldSkip);
        Assert.Equal(
            ["INCOMPLETE_PRODUCT_ROW_SKIPPED"],
            result.Issues.Select(issue => issue.Code));
    }

    [Fact]
    public void NormalizeSkuUsesOneCultureIndependentKeyForAsciiAndTurkishI()
    {
        var ascii = SkuNormalizer.Normalize("3401.BRD.GRV06-K-I");
        var turkish = SkuNormalizer.Normalize("3401.BRD.GRV06-K-İ");

        Assert.Equal(ascii, turkish);
        Assert.NotEqual(ascii, SkuNormalizer.Normalize("3401.BRD.GRV06-K-J"));
    }

    [Fact]
    public void NormalizeSkipsRowWhenProductNameIsMissing()
    {
        var result = ProductImportNormalizer.Normalize(
            CreateRow(new Dictionary<string, string?> { ["SKU"] = "SKU-1", ["Marka"] = "Brand" }));

        Assert.True(result.IsValid);
        Assert.True(result.ShouldSkip);
        Assert.Contains(result.Issues, issue => issue.Code == "EFFECTIVELY_EMPTY_PRODUCT_ROW");
    }

    [Fact]
    public void NormalizeSkipsRowWhenSkuAndProductNameAreMissing()
    {
        var result = ProductImportNormalizer.Normalize(
            CreateRow(new Dictionary<string, string?> { ["Marka"] = "Brand", ["Renk 1"] = "Blue" }));

        Assert.True(result.IsValid);
        Assert.True(result.ShouldSkip);
        Assert.Contains(result.Issues, issue => issue.Code == "EFFECTIVELY_EMPTY_PRODUCT_ROW");
    }

    [Fact]
    public void NormalizeMapsEveryExtendedAttributeAndRelation()
    {
        var result = ProductImportNormalizer.Normalize(CreateRow(new Dictionary<string, string?>
        {
            ["ÜRÜN KODU"] = "sku-1", ["ÜRÜN ADI / UZUN AÇIKLAMA"] = "Ürün",
            ["KOLİ İÇİ ADET"] = "12", ["RENK 1"] = "Kırmızı", ["RENK 2"] = "Mavi",
            ["MODEL/SERİ"] = "Seri X", ["KULLANIM AMACI / KULLANIM YERİ / FİLTRE İÇİN 1"] = "Otel",
            ["ŞEKİL"] = "Yuvarlak", ["HACİM / ÖLÇÜ (CC)"] = "250", ["ÇAP (CM / Ø)"] = "20",
            ["EN (MM)"] = "45", ["BOY (CM)"] = "30", ["YÜKSEKLİK (CM)"] = "8",
            ["ÜRÜN AĞIRLIK (GR)"] = "700", ["ANA GÖRSEL DOSYA ADI"] = "sku-1.webp",
            ["SEO BAŞLIĞI"] = "SEO", ["META AÇIKLAMASI"] = "Meta", ["URL ADRESİ"] = "/urun",
            ["BENZER ÜRÜN KODU 1"] = "sku-2", ["TAMAMLAYICI ÜRÜN KODU 1"] = "sku-3"
        }));

        Assert.True(result.IsValid);
        var attributes = Assert.IsType<Dictionary<string, string[]>>(result.Values["attributeValues"]);
        Assert.Equal(["Kırmızı", "Mavi"], attributes["COLOR"]);
        Assert.Equal(["Seri X"], attributes["MODEL_SERIES"]);
        Assert.Equal(["Otel"], attributes["USAGE_AREA"]);
        Assert.Equal(["20"], attributes["DIAMETER_CM"]);
        Assert.Equal(["700"], attributes["WEIGHT_GR"]);
        Assert.Equal("SEO", result.Values["metaTitle"]);
        Assert.Equal(["SKU-2"], Assert.IsType<string[]>(result.Values["similarSkus"]));
        Assert.Equal(["SKU-3"], Assert.IsType<string[]>(result.Values["complementarySkus"]));
    }

    private static TabularImportRow CreateRow(
        IReadOnlyDictionary<string, string?> values) =>
        new(2, values);
}
