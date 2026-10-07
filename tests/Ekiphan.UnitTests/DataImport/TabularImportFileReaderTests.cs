using System.Text;
using ClosedXML.Excel;
using Ekiphan.Application.DataImport;
using Ekiphan.Infrastructure.DataImport;

namespace Ekiphan.UnitTests.DataImport;

public sealed class TabularImportFileReaderTests
{
    private readonly TabularImportFileReader _reader = new();

    [Fact]
    public async Task ReadAsyncParsesSemicolonCsvAndQuotedNewline()
    {
        const string content =
            "Ürün Kodu;Ürün Adı;Açıklama\r\n" +
            "ABC-1;Tabak;\"İlk satır\r\nİkinci satır\"\r\n";
        await using var stream = Utf8(content);

        var document = await _reader.ReadAsync(stream, "products.csv");

        var row = Assert.Single(Assert.Single(document.Sheets).Rows);
        Assert.Equal(2, row.RowNumber);
        Assert.Equal("ABC-1", row.Values["Ürün Kodu"]);
        Assert.Equal("İlk satır\r\nİkinci satır", row.Values["Açıklama"]);
    }

    [Fact]
    public async Task ReadAsyncParsesXlsxAndPreservesWorksheetRowNumber()
    {
        await using var stream = CreateWorkbook();

        var document = await _reader.ReadAsync(stream, "products.xlsx");

        var sheet = Assert.Single(document.Sheets);
        Assert.Equal("Ürünler", sheet.Name);
        var row = Assert.Single(sheet.Rows);
        Assert.Equal(3, row.RowNumber);
        Assert.Equal("X-10", row.Values["SKU"]);
    }

    [Fact]
    public async Task ReadAsyncRejectsUnsupportedExtension()
    {
        await using var stream = Utf8("anything");

        var exception = await Assert.ThrowsAsync<ImportFileReadException>(
            () => _reader.ReadAsync(stream, "products.xls"));

        Assert.Contains(".csv and .xlsx", exception.Message);
    }

    [Fact]
    public async Task ReadAsyncFindsHeadersAndImportsEveryProductSheet()
    {
        using var workbook = new XLWorkbook();
        workbook.AddWorksheet("Cover").Cell(1, 1).Value = "Notes";
        foreach (var item in new[] { (Name: "One", Sku: "A-1"), (Name: "Two", Sku: "B-1") })
        {
            var sheet = workbook.AddWorksheet(item.Name);
            sheet.Cell(3, 1).Value = "Stok Kodu";
            sheet.Cell(3, 2).Value = "Urun Adi";
            sheet.Cell(4, 1).Value = item.Sku;
            sheet.Cell(4, 2).Value = "Product";
        }
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var document = await _reader.ReadAsync(stream, "products.xlsx");

        Assert.Equal(2, document.Sheets.Count);
        Assert.All(document.Sheets, sheet => Assert.Equal(4, Assert.Single(sheet.Rows).RowNumber));
    }

    [Fact]
    public async Task ReadAsyncRejectsPathInFileName()
    {
        await using var stream = Utf8("anything");

        await Assert.ThrowsAsync<ImportFileReadException>(
            () => _reader.ReadAsync(stream, "../products.csv"));
    }

    [Fact]
    public async Task ReadAsyncEnforcesFileSizeLimit()
    {
        await using var stream = Utf8("SKU,Name\r\nABC,Product");
        var options = new ImportFileReadOptions
        {
            MaximumFileSizeBytes = 5
        };

        var exception = await Assert.ThrowsAsync<ImportFileReadException>(
            () => _reader.ReadAsync(stream, "products.csv", options));

        Assert.Contains("File size exceeds", exception.Message);
    }

    [Fact]
    public async Task ReadAsyncRejectsDuplicateHeaders()
    {
        await using var stream = Utf8("SKU,sku\r\nA,B");

        var exception = await Assert.ThrowsAsync<ImportFileReadException>(
            () => _reader.ReadAsync(stream, "products.csv"));

        Assert.Contains("unique", exception.Message);
    }

    [Fact]
    public async Task ReadAsyncEnforcesTotalRowLimit()
    {
        await using var stream = Utf8("SKU,Name\r\nA,One\r\nB,Two");
        var options = new ImportFileReadOptions
        {
            MaximumRows = 1
        };

        var exception = await Assert.ThrowsAsync<ImportFileReadException>(
            () => _reader.ReadAsync(stream, "products.csv", options));

        Assert.Contains("row count exceeds", exception.Message);
    }

    private static MemoryStream Utf8(string value) =>
        new(Encoding.UTF8.GetBytes(value));

    private static MemoryStream CreateWorkbook()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Ürünler");
        worksheet.Cell(1, 1).Value = "SKU";
        worksheet.Cell(1, 2).Value = "Ürün Adı";
        worksheet.Cell(3, 1).Value = "X-10";
        worksheet.Cell(3, 2).Value = "Kase";
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
