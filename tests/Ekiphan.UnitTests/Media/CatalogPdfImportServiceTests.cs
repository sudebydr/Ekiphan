using System.IO.Compression;
using System.Text;
using Ekiphan.Domain.Media;
using Ekiphan.Infrastructure.CatalogPdfImport;
using Ekiphan.Infrastructure.Media;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
    public async Task PreviewAcceptsAnyValidCatalogSubset()
    {
        await using var zip = CreateZip([CatalogPdfImportService.Manifest[0].FileName]);
        var result = await Service().PreviewAsync(zip, "catalogs.zip", zip.Length);
        Assert.Single(result.Files);
    }

    [Fact]
    public async Task PreviewAcceptsANewDynamicCatalog()
    {
        await using var zip = CreateManifestZip((archive) => Add(archive, "new-catalog.pdf", "%PDF-new"));
        var result = await Service().PreviewAsync(zip, "catalogs.zip", zip.Length);
        Assert.Contains(result.Files, item => item.FileName == "new-catalog.pdf" && item.Status == "Matched");
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

    [Fact]
    public async Task ExecuteRestoresMissingStoredPdfForExistingMediaAsset()
    {
        await using var db = CreateContext();
        var root = Path.Combine(Path.GetTempPath(), $"ekiphan-catalog-test-{Guid.NewGuid():N}");
        try
        {
            var storage = new LocalMediaFileStorage(root);
            foreach (var item in CatalogPdfImportService.Manifest)
            {
                var key = CatalogPdfImportService.StorageKey(item.Slug);
                var asset = MediaAsset.CreateFile(Guid.NewGuid(), MediaAssetType.Pdf, item.FileName, key,
                    "application/pdf", 10, new string('A', 64), "Local");
                asset.AddTranslation("tr", item.Title);
                db.MediaAssets.Add(asset);
                if (item.Slug != "bar")
                    await storage.SaveAsync(key, new MemoryStream([1]));
            }
            await db.SaveChangesAsync();
            await using var zip = CreateManifestZip();

            var result = await new CatalogPdfImportService(db, storage)
                .ExecuteAsync(zip, "catalogs.zip", zip.Length);

            Assert.Equal(1, result.Uploaded);
            Assert.Equal(0, result.Created);
            Assert.Equal(11, result.Skipped);
            Assert.Equal(0, result.Failed);
            Assert.True(File.Exists(Path.Combine(root, "catalogs", "bar", "bar.pdf")));
            Assert.Equal(12, await db.MediaAssets.CountAsync());
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SingleUploadRejectsNonPdfAndFilesOver500Mb()
    {
        await using var db = CreateContext();
        var service = new CatalogPdfImportService(db, new LocalMediaFileStorage(Path.GetTempPath()));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UploadSingleAsync(new MemoryStream("%PDF-"u8.ToArray()),
            "catalog.txt", 5, "Catalog"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UploadSingleAsync(new MemoryStream("%PDF-"u8.ToArray()),
            "catalog.pdf", 500L * 1024 * 1024 + 1, "Catalog"));
    }

    [Fact]
    public async Task SingleUploadPersistsPdfAndReturnsCoverFallbackWhenRenderingFails()
    {
        await using var db = CreateContext();
        var root = Path.Combine(Path.GetTempPath(), $"ekiphan-single-catalog-{Guid.NewGuid():N}");
        try
        {
            var bytes = Encoding.ASCII.GetBytes("%PDF-invalid-body");
            var result = await new CatalogPdfImportService(db, new LocalMediaFileStorage(root))
                .UploadSingleAsync(new MemoryStream(bytes), "yeni-katalog.pdf", bytes.Length, "Yeni Katalog");

            Assert.NotNull(result.Warning);
            Assert.Equal("/images/catalog-placeholder.webp", result.CoverUrl);
            Assert.True(File.Exists(Path.Combine(root, "catalogs", "yeni-katalog", "yeni-katalog.pdf")));
            var asset = Assert.Single(await db.MediaAssets.Where(item => item.AssetType == MediaAssetType.Pdf).ToListAsync());
            Assert.Equal(MediaStatus.Active, asset.Status);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SingleUploadGeneratesAndPersistsWebpCover()
    {
        await using var db = CreateContext();
        var root = Path.Combine(Path.GetTempPath(), $"ekiphan-single-cover-{Guid.NewGuid():N}");
        try
        {
            var bytes = Encoding.ASCII.GetBytes("%PDF-test");
            var service = new CatalogPdfImportService(db, new LocalMediaFileStorage(root), new StubCoverRenderer());
            var result = await service.UploadSingleAsync(new MemoryStream(bytes), "kapakli.pdf", bytes.Length, null);

            Assert.Null(result.Warning);
            Assert.EndsWith("-cover.webp", result.CoverUrl);
            Assert.Equal(2, await db.MediaAssets.CountAsync());
            var cover = await db.MediaAssets.SingleAsync(item => item.AssetType == MediaAssetType.Image);
            Assert.Equal("image/webp", cover.MimeType);
            Assert.True(File.Exists(Path.Combine(root, "catalogs", "kapakli", "kapakli-cover.webp")));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static CatalogPdfImportService Service() => new(null!, null!);
    private static EkiphanDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<EkiphanDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
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

    private sealed class StubCoverRenderer : IPdfCoverRenderer
    {
        public void Render(Stream output, Stream pdf) => output.Write("RIFFxxxxWEBP"u8);
    }
}
