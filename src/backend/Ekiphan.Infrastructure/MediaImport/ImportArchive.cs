using System.IO.Compression;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Readers;

namespace Ekiphan.Infrastructure.MediaImport;

/// <summary>Streaming archive adapter. Never extracts using an archive entry's path.</summary>
public sealed class ImportArchive : IDisposable
{
    private readonly IDisposable archive;
    private IReader? reader;
    private int current = -1;
    private ImportArchive(IDisposable archive) => this.archive = archive;
    public IReadOnlyList<Entry> Entries { get; private set; } = [];

    public static ImportArchive Open(Stream content, string fileName, int maxEntries = 5000,
        long maxExpanded = 2000L * 1024 * 1024, double maxRatio = 100)
    {
        if (!content.CanSeek) throw new ArgumentException("Arşiv akışı seekable olmalı.");
        content.Position = 0;
        if (Path.GetExtension(fileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            if (ZipProductMediaImportArchiveReader.HasEncryptedEntries(content))
                throw new ArgumentException("Encrypted or corrupt ZIP files are not allowed.");
            content.Position = 0;
            var zip = new ZipArchive(content, ZipArchiveMode.Read, true);
            var result = new ImportArchive(zip);
            result.Entries = zip.Entries.Select(e => new Entry(e.FullName, e.Length, e.CompressedLength, false, e.Open)).ToArray();
            return result;
        }
        if (!Path.GetExtension(fileName).Equals(".rar", StringComparison.OrdinalIgnoreCase) || !RarArchive.IsRarFile(content))
            throw new ArgumentException("Geçerli bir ZIP veya RAR arşivi gerekli.");
        content.Position = 0;
        IRarArchive? rar = null;
        try
        {
            rar = RarArchive.OpenArchive(content, new ReaderOptions { LeaveStreamOpen = true });
            var entries = rar.Entries.Take(maxEntries + 1).ToArray();
            if (entries.Length > maxEntries) throw new ArgumentException("Arşiv kayıt sayısı sınırı aşıldı.");
            if (entries.Any(e => e.IsEncrypted || !e.IsComplete || e.IsSplitAfter || e.LinkTarget is not null))
                throw new ArgumentException("Şifreli, bağlantı içeren veya eksik çok parçalı RAR kabul edilmez.");
            long expanded = 0;
            foreach (var entry in entries)
            {
                if (entry.Size < 0 || entry.Size > maxExpanded - expanded)
                    throw new ArgumentException("Arşivin açılmış boyut sınırı aşıldı.");
                expanded += entry.Size;
            }
            if (expanded / Math.Max(1d, content.Length) > maxRatio)
                throw new ArgumentException("Arşiv sıkıştırma oranı güvenli değil.");
            var result = new ImportArchive(rar);
            result.Entries = entries.Select((e, index) => new Entry(e.IsDirectory ? (e.Key ?? string.Empty).TrimEnd('/') + "/" : e.Key ?? string.Empty,
                e.Size, e.CompressedSize, rar.IsSolid, () => rar.IsSolid ? result.OpenRar(index, e.Size) : new LimitedStream(e.OpenEntryStream(), e.Size))).ToArray();
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            rar?.Dispose();
            throw new ArgumentException("RAR okunamadı; bozuk, şifreli, eksik veya güvenlik limitlerini aşan arşiv kabul edilmez.", ex);
        }
    }

    private LimitedStream OpenRar(int index, long length)
    {
        reader ??= ((IRarArchive)archive).ExtractAllEntries();
        if (index <= current) throw new InvalidDataException("RAR girdileri sırayla okunmalı.");
        while (current < index)
        {
            if (!reader.MoveToNextEntry()) throw new InvalidDataException("RAR girdisi eksik.");
            current++;
        }
        return new LimitedStream(reader.OpenEntryStream(), length);
    }
    public void Dispose() { reader?.Dispose(); archive.Dispose(); }

    public sealed record Entry(string FullName, long Length, long CompressedLength, bool IsSolid, Func<Stream> OpenStream)
    {
        public string Name => FullName.EndsWith('/') ? string.Empty : Path.GetFileName(FullName);
        public Stream Open() => OpenStream();
    }

    private sealed class LimitedStream(Stream source, long remaining) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => Check(source.Read(buffer, offset, (int)Math.Min(count, remaining + 1)));
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            Check(await source.ReadAsync(buffer[..(int)Math.Min(buffer.Length, remaining + 1)], cancellationToken));
        private int Check(int read)
        {
            remaining -= read;
            if (remaining < 0 || read == 0 && remaining > 0) throw new InvalidDataException("RAR girdi boyutu başlıkla uyuşmuyor.");
            return read;
        }
        protected override void Dispose(bool disposing) { if (disposing) source.Dispose(); base.Dispose(disposing); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
