using System.Text;
using Ekiphan.Application.Media;
using Ekiphan.Application.MediaImport;
using Ekiphan.Infrastructure.Media;
using Ekiphan.Infrastructure.MediaImport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Ekiphan.UnitTests.Media;

public sealed class ImportArchiveTests
{
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScL0WQAAAABJRU5ErkJggg==");

    [Fact]
    public async Task SolidRarCanSkipAnEntryAndReadSubsequentImagesInOrder()
    {
        var bytes = Rar(("skip.png", Png), ("one.png", Png), ("two.png", Png));
        bytes[10] |= 8; // Main header SOLID flag.
        BitConverter.GetBytes((ushort)Crc(bytes.AsSpan(9, 11))).CopyTo(bytes, 7);
        var position = 20;
        for (var index = 0; index < 3; index++)
        {
            var size = BitConverter.ToUInt16(bytes, position + 5);
            if (index > 0) bytes[position + 3] |= 0x10;
            BitConverter.GetBytes((ushort)Crc(bytes.AsSpan(position + 2, size - 2))).CopyTo(bytes, position);
            position += size + Png.Length;
        }
        using var source = new MemoryStream(bytes);
        using var archive = ImportArchive.Open(source, "solid.rar");
        foreach (var entry in archive.Entries.Skip(1))
        {
            using var image = entry.Open();
            using var output = new MemoryStream();
            await image.CopyToAsync(output);
            Assert.Equal(Png, output.ToArray());
        }
    }

    [Fact]
    public async Task RarImagesUseExistingValidationAndUnsafeEntryIsNeverExtracted()
    {
        var storage = new Storage();
        var reader = new ZipProductMediaImportArchiveReader(Options.Create(new ProductMediaImportOptions()), storage,
            new ProductMediaSkuParser(), new Sha256ProductMediaDuplicateDetector(), new MediaFileSignatureValidator(),
            NullLogger<ZipProductMediaImportArchiveReader>.Instance);
        using var source = new MemoryStream(Rar(("../unsafe.png", Png), ("SKU-1.png", Png), ("SKU-2.png", Png)));
        var result = await reader.ReadAsync("test", source, "test.rar", "application/vnd.rar", source.Length);
        Assert.Equal(2, result.Entries.Count(e => e.Status == ProductMediaPreviewFileStatus.Ready));
        var unsafeEntry = Assert.Single(result.Entries, e => e.ErrorCode == "UNSAFE_ARCHIVE_ENTRY");
        Assert.DoesNotContain(unsafeEntry.TemporaryFileId, storage.Files.Keys);
    }

    [Fact]
    public async Task RarPdfPreviewUsesExistingCatalogPipeline()
    {
        using var source = new MemoryStream(Rar(("new-catalog.pdf", "%PDF-1.7 synthetic"u8.ToArray())));
        await using var db = new Ekiphan.Infrastructure.Persistence.EkiphanDbContext(
            new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Ekiphan.Infrastructure.Persistence.EkiphanDbContext>().Options);
        var service = new Ekiphan.Infrastructure.CatalogPdfImport.CatalogPdfImportService(db, new MediaStorage());
        var preview = await service.PreviewAsync(source, "catalog.rar", source.Length);
        Assert.Equal(1, preview.PdfCount);
        Assert.Equal("Matched", Assert.Single(preview.Files).Status);
        Assert.True(source.CanRead);
    }

    [Fact]
    public void RarCountExpandedSizeEncryptionAndInvalidSignatureAreRejected()
    {
        var bytes = Rar(("one.png", Png), ("two.png", Png));
        using var count = new MemoryStream(bytes);
        Assert.Throws<ArgumentException>(() => ImportArchive.Open(count, "test.rar", maxEntries: 1));
        using var size = new MemoryStream(bytes);
        Assert.Throws<ArgumentException>(() => ImportArchive.Open(size, "test.rar", maxExpanded: 1));
        using var corrupt = new MemoryStream("not a rar"u8.ToArray());
        Assert.Throws<ArgumentException>(() => ImportArchive.Open(corrupt, "test.rar"));
        var encrypted = Rar(("one.png", Png));
        // File header flags at offset 23. Recompute the header CRC after setting password protection.
        encrypted[23] |= 4;
        var headerSize = BitConverter.ToUInt16(encrypted, 25);
        BitConverter.GetBytes((ushort)Crc(encrypted.AsSpan(22, headerSize - 2))).CopyTo(encrypted, 20);
        using var password = new MemoryStream(encrypted);
        Assert.Throws<ArgumentException>(() => ImportArchive.Open(password, "test.rar"));
    }

    // Minimal RAR4 stored-entry fixture, generated in memory. No external archives or executables.
    private static byte[] Rar(params (string Name, byte[] Bytes)[] files)
    {
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output, Encoding.UTF8, true);
        writer.Write(new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00 });
        Header(writer, 0x73, 0, body => { body.Write((ushort)0); body.Write(0u); });
        foreach (var (name, bytes) in files)
        {
            var encoded = Encoding.UTF8.GetBytes(name);
            Header(writer, 0x74, 0x8000, body => {
                body.Write((uint)bytes.Length); body.Write((uint)bytes.Length); body.Write((byte)2);
                body.Write(Crc(bytes)); body.Write(0u); body.Write((byte)20); body.Write((byte)0x30);
                body.Write((ushort)encoded.Length); body.Write(0x20u); body.Write(encoded);
            });
            writer.Write(bytes);
        }
        Header(writer, 0x7B, 0, _ => { });
        return output.ToArray();
    }
    private static void Header(BinaryWriter output, byte type, ushort flags, Action<BinaryWriter> body)
    {
        using var bytes = new MemoryStream();
        using var writer = new BinaryWriter(bytes, Encoding.UTF8, true);
        writer.Write((ushort)0); writer.Write(type); writer.Write(flags); writer.Write((ushort)0); body(writer);
        var buffer = bytes.ToArray();
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 5);
        BitConverter.GetBytes((ushort)Crc(buffer.AsSpan(2))).CopyTo(buffer, 0);
        output.Write(buffer);
    }
    private static uint Crc(ReadOnlySpan<byte> bytes)
    {
        var crc = uint.MaxValue;
        foreach (var value in bytes) { crc ^= value; for (var bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ ((crc & 1) == 0 ? 0 : 0xEDB88320u); }
        return ~crc;
    }
    private sealed class Storage : ITemporaryProductMediaStorage
    {
        public Dictionary<string, byte[]> Files { get; } = [];
        public async Task StoreFileAsync(string containerId, string temporaryFileId, Stream content, CancellationToken cancellationToken = default)
        { using var output = new MemoryStream(); await content.CopyToAsync(output, cancellationToken); Files[temporaryFileId] = output.ToArray(); }
        public Task<Stream> OpenReadAsync(string containerId, string temporaryFileId, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream(Files[temporaryFileId]));
        public Task DeleteAsync(string containerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
    private sealed class MediaStorage : IMediaFileStorage
    {
        public bool IsConfigured => true;
        public Task SaveAsync(string key, Stream content, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Preview must not persist media.");
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Preview must not delete media.");
    }
}
