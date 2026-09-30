using System.IO.Compression;
using System.Text;
using Ekiphan.Infrastructure.CatalogPdfImport;

namespace Ekiphan.UnitTests.Media;

public sealed class CatalogPdfImportServiceTests
{
    [Fact]
    public async Task PreviewAcceptsTheApprovedPdfManifest()
    {
        await using var zip = CreateManifestZip();
        var result = await Service().PreviewAsync(zip, "catalogs.zip", zip.Length);
        Assert.Equal(12, result.PdfCount);
        Assert.All(result.Files, file => Assert.Equal("Matched", file.Status));
    }

    [Fact]
    public async Task PreviewRejectsNonPdfEntries()
    {
        await using var zip = CreateManifestZip((archive) => Add(archive, "notes.txt", "not a pdf"));
        await Assert.ThrowsAsync<ArgumentException>(() => Service().PreviewAsync(zip, "catalogs.zip", zip.Length));
    }

    [Fact]
    public async Task PreviewIgnoresMacosxAndAppleDoubleEntries()
    {
        await using var zip = CreateManifestZip((archive) => { Add(archive, "__MACOSX/._catalogs", "metadata"); Add(archive, "__MACOSX/a/._file.pdf", "metadata"); });
        var result = await Service().PreviewAsync(zip, "catalogs.zip", zip.Length);
        Assert.Equal(2, result.IgnoredCount);
    }

    [Fact]
    public async Task PreviewRejectsUnsafePaths()
    {
        await using var zip = CreateManifestZip((archive) => Add(archive, "../evil.pdf", "%PDF-evil"));
        await Assert.ThrowsAsync<ArgumentException>(() => Service().PreviewAsync(zip, "catalogs.zip", zip.Length));
    }

    [Fact]
    public async Task PreviewRejectsDuplicateNormalizedNames()
    {
        await using var zip = CreateManifestZip((archive) => Add(archive, "nested/Ekiphan-bar.pdf", "%PDF-duplicate"));
        await Assert.ThrowsAsync<ArgumentException>(() => Service().PreviewAsync(zip, "catalogs.zip", zip.Length));
    }

    [Fact]
    public void ManifestMapsAllTwelveExistingCatalogCards()
    {
        Assert.Equal(12, CatalogPdfImportService.Manifest.Count);
        Assert.Equal(["acik-bufe", "bar", "cihazlar", "restaurant", "klasik", "kutahya", "bonna", "esma", "fabrika", "nude", "pasabahce", "selene"], CatalogPdfImportService.Manifest.Select(x => x.Slug));
    }

    [Fact]
    public async Task PreviewRejectsMissingCatalogs()
    {
        await using var zip = CreateZip([CatalogPdfImportService.Manifest[0].FileName]);
        await Assert.ThrowsAsync<ArgumentException>(() => Service().PreviewAsync(zip, "catalogs.zip", zip.Length));
    }

    [Fact]
    public async Task PreviewRejectsAnUnmatchedPdf()
    {
        await using var zip = CreateManifestZip((archive) => Add(archive, "new-catalog.pdf", "%PDF-new"));
        await Assert.ThrowsAsync<ArgumentException>(() => Service().PreviewAsync(zip, "catalogs.zip", zip.Length));
    }

    [Fact]
    public async Task PreviewValidatesThePdfSignatureWithoutReadingFullEntries()
    {
        await using var invalidZip = CreateManifestZip((archive) => { });
        invalidZip.Position = 0;
        // Replace one valid entry with a non-PDF signature; the service reads only the fixed 5-byte signature.
        await using var zip = CreateZip(CatalogPdfImportService.Manifest.Select(x => x.FileName), invalidName: "Ekiphan-bar.pdf");
        await Assert.ThrowsAsync<ArgumentException>(() => Service().PreviewAsync(zip, "catalogs.zip", zip.Length));
    }

    private static CatalogPdfImportService Service() => new(null!, null!);
    private static MemoryStream CreateManifestZip(Action<ZipArchive>? add = null)
    {
        var stream = CreateZip(CatalogPdfImportService.Manifest.Select(x => x.FileName));
        stream.Position = 0;
        if (add is null) return stream;
        var result = new MemoryStream();
        using var archive = new ZipArchive(result, ZipArchiveMode.Create, true);
        foreach (var file in CatalogPdfImportService.Manifest) Add(archive, file.FileName, "%PDF-valid");
        add(archive); archive.Dispose(); result.Position = 0; stream.Dispose(); return result;
    }
    private static MemoryStream CreateZip(IEnumerable<string> names, string? invalidName = null)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            foreach (var name in names) Add(archive, name, name == invalidName ? "NOTPDF" : "%PDF-valid");
        stream.Position = 0; return stream;
    }
    private static void Add(ZipArchive archive, string name, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name).Open(), Encoding.ASCII);
        writer.Write(content);
    }
}
