using System.IO.Compression;
using System.Text;
using System.Globalization;
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

    [Fact]
    public async Task BackfillRepairsMissingCoverWithoutDuplicatingRecordAndIsIdempotent()
    {
        await using var db = CreateContext();
        var root = Path.Combine(Path.GetTempPath(), $"ekiphan-cover-repair-{Guid.NewGuid():N}");
        try
        {
            var service = new CatalogPdfImportService(db, new LocalMediaFileStorage(root), new StubCoverRenderer());
            var bytes = Encoding.ASCII.GetBytes("%PDF-test");
            var result = await service.UploadSingleAsync(new MemoryStream(bytes), "repair.pdf", bytes.Length, null);
            File.Delete(Path.Combine(root, "catalogs", "repair", "repair-cover.webp"));
            Assert.Equal("/images/catalog-placeholder.webp", Assert.Single(await service.GetPublicDocumentsAsync()).CoverUrl);
            var backfill = await service.BackfillCoversAsync();
            Assert.Equal(1, backfill.Created);
            Assert.Equal(0, backfill.Failed);
            Assert.Equal(2, await db.MediaAssets.CountAsync());
            Assert.Equal(1, (await service.BackfillCoversAsync()).Skipped);
            Assert.Equal(result.CoverUrl, Assert.Single(await service.GetPublicDocumentsAsync()).CoverUrl);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task ReplacementKeepsPdfIdentityAndOriginalFileButRefreshesCoverUrl()
    {
        await using var db = CreateContext();
        var root = Path.Combine(Path.GetTempPath(), $"ekiphan-cover-replace-{Guid.NewGuid():N}");
        try
        {
            var service = new CatalogPdfImportService(db, new LocalMediaFileStorage(root), new StubCoverRenderer());
            var bytes = Encoding.ASCII.GetBytes("%PDF-test");
            var original = await service.UploadSingleAsync(new MemoryStream(bytes), "replace.pdf", bytes.Length, null);
            var changed = Encoding.ASCII.GetBytes("%PDF-changed");
            var result = await service.ReplaceAsync(original.MediaAssetId, new MemoryStream(changed), "updated.pdf", changed.Length, "Updated");
            Assert.Equal(original.MediaAssetId, result.MediaAssetId);
            Assert.NotEqual(original.CoverUrl, result.CoverUrl);
            Assert.Null(result.Warning);
            Assert.True(File.Exists(Path.Combine(root, "catalogs", "replace", "replace.pdf")));
            Assert.Single(await db.MediaAssets.Where(x => x.AssetType == MediaAssetType.Pdf).ToListAsync());
            Assert.Equal(result.CoverUrl, Assert.Single(await service.GetPublicDocumentsAsync()).CoverUrl);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task ZipUploadUsesTheSharedCoverRenderer()
    {
        await using var db = CreateContext();
        var root = Path.Combine(Path.GetTempPath(), $"ekiphan-cover-zip-{Guid.NewGuid():N}");
        try
        {
            var service = new CatalogPdfImportService(db, new LocalMediaFileStorage(root), new StubCoverRenderer());
            await using var zip = CreateZip(["synthetic.pdf"]);
            var result = await service.ExecuteAsync(zip, "synthetic.zip", zip.Length);
            Assert.Equal(1, result.Created);
            Assert.EndsWith("-cover.webp", Assert.Single(await service.GetPublicDocumentsAsync()).CoverUrl);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task IdenticalCoversAreReusedAcrossDifferentPdfsWithoutDuplicateHashRecords()
    {
        await using var db = CreateContext();
        var root = Path.Combine(Path.GetTempPath(), $"ekiphan-cover-reuse-{Guid.NewGuid():N}");
        try
        {
            var service = new CatalogPdfImportService(db, new LocalMediaFileStorage(root), new ConstantCoverRenderer());
            var bytes = Encoding.ASCII.GetBytes("%PDF-test");
            var first = await service.UploadSingleAsync(new MemoryStream(bytes), "first.pdf", bytes.Length, null);
            var secondBytes = Encoding.ASCII.GetBytes("%PDF-different");
            var second = await service.UploadSingleAsync(new MemoryStream(secondBytes), "second.pdf", secondBytes.Length, null);
            Assert.Equal(first.CoverUrl, second.CoverUrl);
            Assert.Single(await db.MediaAssets.Where(x => x.AssetType == MediaAssetType.Image).ToListAsync());
            Assert.All(await service.GetPublicDocumentsAsync(), x => Assert.Equal(first.CoverUrl, x.CoverUrl));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task BackfillContinuesAfterAnUnreadablePdf()
    {
        await using var db = CreateContext();
        var root = Path.Combine(Path.GetTempPath(), $"ekiphan-cover-continue-{Guid.NewGuid():N}");
        try
        {
            var service = new CatalogPdfImportService(db, new LocalMediaFileStorage(root), new StubCoverRenderer());
            var bytes = Encoding.ASCII.GetBytes("%PDF-test");
            var first = await service.UploadSingleAsync(new MemoryStream(bytes), "missing.pdf", bytes.Length, null);
            var secondBytes = Encoding.ASCII.GetBytes("%PDF-different");
            await service.UploadSingleAsync(new MemoryStream(secondBytes), "good.pdf", secondBytes.Length, null);
            File.Delete(Path.Combine(root, "catalogs", "missing", "missing.pdf"));
            File.Delete(Path.Combine(root, "catalogs", "missing", "missing-cover.webp"));
            File.Delete(Path.Combine(root, "catalogs", "good", "good-cover.webp"));
            var result = await service.BackfillCoversAsync();
            Assert.Equal(1, result.Failed);
            Assert.Equal(1, result.Created);
            Assert.EndsWith("-cover.webp", (await service.GetPublicDocumentsAsync()).Single(x => x.Id != first.MediaAssetId).CoverUrl);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task RealFirstPageRendererProducesDecodableWebp()
    {
        await using var db = CreateContext();
        var root = Path.Combine(Path.GetTempPath(), $"ekiphan-real-cover-{Guid.NewGuid():N}");
        try
        {
            var objects = new[] {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 100 200] /Contents 4 0 R >>",
                "<< /Length 0 >>\nstream\n\nendstream"
            };
            var pdf = new StringBuilder("%PDF-1.4\n");
            var offsets = new List<int>();
            for (var i = 0; i < objects.Length; i++) { offsets.Add(pdf.Length); pdf.Append(CultureInfo.InvariantCulture, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n"); }
            var xref = pdf.Length;
            pdf.Append("xref\n0 5\n0000000000 65535 f \n");
            foreach (var offset in offsets) pdf.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");
            pdf.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
            var bytes = Encoding.ASCII.GetBytes(pdf.ToString());
            var result = await new CatalogPdfImportService(db, new LocalMediaFileStorage(root))
                .UploadSingleAsync(new MemoryStream(bytes), "real.pdf", bytes.Length, "Real");
            Assert.Null(result.Warning);
            var cover = await db.MediaAssets.SingleAsync(x => x.AssetType == MediaAssetType.Image);
            using var image = SixLabors.ImageSharp.Image.Load(Path.Combine(root, cover.StorageKey!));
            Assert.True(image.Width > 0);
            Assert.Equal(2, image.Height / image.Width);
            Assert.Equal(cover.Id, (await db.MediaAssets.SingleAsync(x => x.AssetType == MediaAssetType.Pdf)).CoverMediaAssetId);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
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
        public void Render(Stream output, Stream pdf)
        {
            output.Write("RIFFxxxxWEBP"u8);
            pdf.CopyTo(output);
        }
    }

    private sealed class ConstantCoverRenderer : IPdfCoverRenderer
    {
        public void Render(Stream output, Stream pdf) => output.Write("RIFFxxxxWEBP"u8);
    }
}
