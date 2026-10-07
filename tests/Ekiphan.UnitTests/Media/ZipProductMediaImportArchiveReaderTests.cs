using System.IO.Compression;
using Ekiphan.Application.Media;
using Ekiphan.Application.MediaImport;
using Ekiphan.Infrastructure.MediaImport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Ekiphan.UnitTests.Media;

public sealed class ZipProductMediaImportArchiveReaderTests
{
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScL0WQAAAABJRU5ErkJggg==");

    [Fact]
    public async Task UnsafeEntryIsSkippedWhileSafeImagesContinue()
    {
        var result = await ReadAsync(("../unsafe.png", Png), ("SKU-1.png", Png), ("SKU-2.png", Png));

        Assert.Equal(3, result.TotalEntries);
        Assert.Equal(2, result.Entries.Count(x => x.Status == ProductMediaPreviewFileStatus.Ready));
        Assert.Contains(result.Entries, x => x.ErrorCode == "UNSAFE_ARCHIVE_ENTRY" &&
            x.Status == ProductMediaPreviewFileStatus.Skipped);
    }

    [Theory]
    [InlineData("/absolute.png")]
    [InlineData("C:/drive.png")]
    [InlineData("folder\\backslash.png")]
    public async Task UnsafePathsAreNeverExtracted(string path)
    {
        var storage = new TestStorage();
        var result = await ReadAsync(storage, (path, Png));

        Assert.Single(result.Entries);
        Assert.Equal("UNSAFE_ARCHIVE_ENTRY", result.Entries[0].ErrorCode);
        Assert.DoesNotContain(result.Entries[0].TemporaryFileId, storage.Files.Keys);
    }

    [Fact]
    public async Task SystemMetadataAndUnsupportedEntriesAreSkippedWhileImageContinues()
    {
        var result = await ReadAsync(("__MACOSX/meta", Png), (".DS_Store", Png), ("Thumbs.db", Png),
            ("readme.txt", "not an image"u8.ToArray()), ("SKU.png", Png));

        Assert.Equal(1, result.Entries.Count(x => x.Status == ProductMediaPreviewFileStatus.Ready));
        Assert.Equal(3, result.Entries.Count(x => x.ErrorCode == "SYSTEM_METADATA"));
        Assert.Contains(result.Entries, x => x.ErrorCode == "UNSUPPORTED_MEDIA_ENTRY" &&
            x.Status == ProductMediaPreviewFileStatus.Skipped);
    }

    [Fact]
    public async Task InvalidImageIsReportedWithoutStoppingOtherImages()
    {
        var result = await ReadAsync(("bad.png", "not a png"u8.ToArray()), ("SKU.png", Png));

        Assert.Contains(result.Entries, x => x.ErrorCode == "INVALID_IMAGE" &&
            x.Status == ProductMediaPreviewFileStatus.Invalid);
        Assert.Contains(result.Entries, x => x.Status == ProductMediaPreviewFileStatus.Ready);
    }

    [Fact]
    public async Task ArchiveWideExtractedSizeLimitStillRejectsArchive()
    {
        var options = new ProductMediaImportOptions { MaxExtractedSizeMb = 0 };

        await Assert.ThrowsAsync<ProductMediaImportSecurityException>(() =>
            ReadAsync(new TestStorage(), options, ("SKU.png", Png)));
    }

    private static Task<ProductMediaArchiveReadResult> ReadAsync(params (string Path, byte[] Content)[] entries) =>
        ReadAsync(new TestStorage(), new ProductMediaImportOptions(), entries);

    private static Task<ProductMediaArchiveReadResult> ReadAsync(TestStorage storage, params (string Path, byte[] Content)[] entries) =>
        ReadAsync(storage, new ProductMediaImportOptions(), entries);

    private static async Task<ProductMediaArchiveReadResult> ReadAsync(TestStorage storage, ProductMediaImportOptions options,
        params (string Path, byte[] Content)[] entries)
    {
        var reader = new ZipProductMediaImportArchiveReader(Options.Create(options), storage, new ProductMediaSkuParser(),
            new Sha256ProductMediaDuplicateDetector(), new MediaFileSignatureValidator(), NullLogger<ZipProductMediaImportArchiveReader>.Instance);
        await using var archive = new MemoryStream(CreateZip(entries));
        return await reader.ReadAsync("test", archive, "images.zip", "application/zip", archive.Length);
    }

    private static byte[] CreateZip(IEnumerable<(string Path, byte[] Content)> entries)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, content) in entries)
            {
                var entry = archive.CreateEntry(path);
                using var stream = entry.Open();
                stream.Write(content);
            }
        }
        return output.ToArray();
    }

    private sealed class TestStorage : ITemporaryProductMediaStorage
    {
        public Dictionary<string, byte[]> Files { get; } = new(StringComparer.Ordinal);

        public async Task StoreFileAsync(string containerId, string temporaryFileId, Stream content, CancellationToken cancellationToken = default)
        {
            await using var output = new MemoryStream();
            await content.CopyToAsync(output, cancellationToken);
            Files[temporaryFileId] = output.ToArray();
        }

        public Task<Stream> OpenReadAsync(string containerId, string temporaryFileId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream(Files[temporaryFileId], writable: false));

        public Task DeleteAsync(string containerId, CancellationToken cancellationToken = default)
        {
            Files.Clear();
            return Task.CompletedTask;
        }
    }
}
