using System.Security.Cryptography;
using System.Text.Json;

namespace Ekiphan.Api.Media;

public sealed class ZipUploadOptions
{
    public string Root { get; set; } = Path.Combine(Path.GetTempPath(), "ekiphan-zip-uploads");
    public long ReservedBytesLimit { get; set; } = 32L * 1024 * 1024 * 1024;
    public int IdleUploadHours { get; set; } = 168;
}

public sealed class ZipUploadException(string code, int status, string message) : InvalidOperationException(message)
{
    public string Code { get; } = code;
    public int Status { get; } = status;
}

public sealed record ZipUploadSummary(Guid Id, string Kind, string FileName, long Length, long Offset,
    string Phase, string? Operation, DateTimeOffset ExpiresAt, bool CanClose);

public sealed record ZipChunk(long Offset, int Length, string Sha256);
public sealed record ZipUploadState(Guid Id, Guid Owner, string Kind, string FileName, long Length,
    string Fingerprint, DateTimeOffset ExpiresAt, long Offset = 0, string Phase = "uploading",
    string? Operation = null, JsonElement? Result = null, string? Error = null,
    IReadOnlyList<ZipChunk>? Chunks = null, bool ArchiveReleased = false,
    IReadOnlyDictionary<string, string>? Fields = null);

/// <summary>Private disk spool. Acknowledged offsets survive restarts; incomplete chunks do not.</summary>
public sealed class ZipUploadStore(ZipUploadOptions options, ILogger<ZipUploadStore>? logger = null) : IDisposable
{
    public const int ChunkBytes = 4 * 1024 * 1024;
    public const long CatalogMaximumBytes = 1536L * 1024 * 1024;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Dictionary<Guid, int> receiving = [];
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
    public void Dispose() => gate.Dispose();
    public FileStream AcquireProcessLease()
    {
        Directory.CreateDirectory(root);
        return new FileStream(Path.Combine(root, "worker.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }
    private readonly string root = Path.GetFullPath(options.Root);

    private string Folder(Guid id) => Path.Combine(root, id.ToString("N"));
    public string ArchivePath(Guid id) => Path.Combine(Folder(id), "archive.zip");
    private string Metadata(Guid id) => Path.Combine(Folder(id), "state.json");

    public async Task<ZipUploadState> CreateAsync(Guid owner, string kind, string fileName, long length,
        string fingerprint, long maximumBytes, CancellationToken ct)
    {
        if (owner == Guid.Empty || kind is not ("catalog" or "product" or "image" or "pdf") || string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(fingerprint) ||
            length <= 0 || length > maximumBytes || !HasAllowedExtension(kind, fileName) ||
            fileName.Length > 250 || fingerprint.Length != 64 || !fingerprint.All(Uri.IsHexDigit))
            throw new ArgumentException("Geçerli bir ZIP dosyası ve dosya kimliği gereklidir; boyut sınırını kontrol edin.");
        await gate.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(root);
            CleanupExpired();
            var states = ReadAll().ToArray();
            var same = states.FirstOrDefault(s => s.Owner == owner && s.Kind == kind && s.Length == length &&
                s.Fingerprint == fingerprint && s.Phase != "closed" && s.ExpiresAt > DateTimeOffset.UtcNow);
            if (same is not null) return same;
            var retained = states.Where(s => !s.ArchiveReleased).ToArray();
            if (retained.Sum(s => s.Length) + length > options.ReservedBytesLimit)
                throw new ZipUploadException("UPLOAD_DISK_QUOTA", 507, "Geçici ZIP disk rezervasyonu dolu. Kendi kullanılmayan yüklemelerinizi kapatın veya yöneticiyle görüşün.");
            var free = new DriveInfo(Path.GetPathRoot(root)!).AvailableFreeSpace;
            if (free < retained.Sum(s => s.Length - s.Offset) + length + 512L * 1024 * 1024)
                throw new ZipUploadException("UPLOAD_DISK_SPACE", 507, "Sunucunun geçici diskinde yeterli boş alan yok. Yöneticiyle görüşün.");
            var state = new ZipUploadState(Guid.NewGuid(), owner, kind, Path.GetFileName(fileName.Replace('\\', '/')),
                length, fingerprint, DateTimeOffset.UtcNow.AddHours(options.IdleUploadHours), Chunks: []);
            Directory.CreateDirectory(Folder(state.Id));
            await SaveAsync(state, ct);
            return state;
        }
        finally { gate.Release(); }
    }

    private static bool HasAllowedExtension(string kind, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return kind switch { "image" => extension is ".jpg" or ".jpeg" or ".png" or ".webp",
            "pdf" => extension == ".pdf", _ => extension is ".zip" or ".rar" };
    }

    public async Task<ZipUploadState> GetAsync(Guid id, Guid owner, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            var state = Read(id, owner);
            if (state.Phase == "uploading") {
                state = state with { ExpiresAt = DateTimeOffset.UtcNow.AddHours(options.IdleUploadHours) };
                await SaveAsync(state, ct);
            }
            return state;
        }
        finally { gate.Release(); }
    }

    public async Task<ZipUploadState> AppendAsync(Guid id, Guid owner, long offset, int length,
        string sha256, Stream body, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            if (Read(id, owner).Phase != "uploading") throw new InvalidOperationException("Aktarım kapatılmış.");
            receiving[id] = receiving.GetValueOrDefault(id) + 1;
        }
        finally { gate.Release(); }
        try { return await AppendCoreAsync(id, owner, offset, length, sha256, body, ct); }
        finally
        {
            await gate.WaitAsync(CancellationToken.None);
            try { if (--receiving[id] == 0) receiving.Remove(id); }
            finally { gate.Release(); }
        }
    }

    private async Task<ZipUploadState> AppendCoreAsync(Guid id, Guid owner, long offset, int length,
        string sha256, Stream body, CancellationToken ct)
    {
        if (length <= 0 || length > ChunkBytes || string.IsNullOrWhiteSpace(sha256) || sha256.Length != 64 || !sha256.All(Uri.IsHexDigit))
            throw new ArgumentException("Geçersiz yükleme parçası.");
        // Receive only this small chunk before taking the metadata lock. A slow client must
        // not block every other user's status/transfer. RAM is bounded by 4 MB per request.
        await GetAsync(id, owner, ct);
        var chunk = new byte[length];
        try { await body.ReadExactlyAsync(chunk, ct); }
        catch (EndOfStreamException ex) { throw new ArgumentException("Yükleme parçası eksik.", ex); }
        var extra = new byte[1];
        if (await body.ReadAsync(extra, ct) != 0 ||
            !Convert.ToHexString(SHA256.HashData(chunk)).Equals(sha256, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Parça boyutu veya SHA-256 doğrulaması başarısız.");
        await gate.WaitAsync(ct);
        try
        {
            var state = Read(id, owner);
            if (state.Phase != "uploading") throw new InvalidOperationException("Aktarım kapatılmış.");
            // Lost acknowledgements may replay an already committed chunk, but never replace it.
            var previous = state.Chunks!.FirstOrDefault(c => c.Offset == offset);
            if (previous is not null)
            {
                if (previous.Length != length || !previous.Sha256.Equals(sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Parça içeriği önceki yüklemeyle uyuşmuyor.");
                return state;
            }
            if (offset != state.Offset || length > state.Length - offset)
                throw new InvalidOperationException("Yükleme konumu uyuşmuyor; kaldığı yeri yeniden kontrol edin.");
            await using var file = new FileStream(ArchivePath(id), FileMode.OpenOrCreate, FileAccess.Write,
                FileShare.None, 65536, FileOptions.Asynchronous);
            file.SetLength(state.Offset); // Discard an interrupted/unacknowledged tail.
            file.Position = state.Offset;
            try
            {
                await file.WriteAsync(chunk, ct);
                await file.FlushAsync(ct);
                file.Flush(flushToDisk: true);
                var next = state with { Offset = offset + length, ExpiresAt = DateTimeOffset.UtcNow.AddHours(options.IdleUploadHours),
                    Chunks = [.. state.Chunks!, new ZipChunk(offset, length, sha256.ToUpperInvariant())] };
                await SaveAsync(next, ct);
                return next;
            }
            catch { file.SetLength(state.Offset); throw; }
        }
        finally { gate.Release(); }
    }

    public async Task<ZipUploadState> BeginAsync(Guid id, Guid owner, string operation, CancellationToken ct,
        IReadOnlyDictionary<string, string>? fields = null)
    {
        await gate.WaitAsync(ct);
        try
        {
            var state = Read(id, owner);
            if (state.Operation == operation && state.Phase is "processing" or "completed" or "failed") return state;
            if (state.Offset != state.Length || operation is not ("preview" or "validate" or "execute") ||
                (operation == "preview" && (state.Phase != "uploading" || state.Kind is "image" or "pdf")) ||
                (operation == "validate" && (state.Kind != "product" || state.Phase != "completed" || state.Operation != "preview")) ||
                (operation == "execute" && (state.Kind is "image" or "pdf" ? state.Phase != "uploading" :
                    state.Phase != "completed" || state.Operation != (state.Kind == "product" ? "validate" : "preview"))))
                throw new InvalidOperationException("Yükleme eksik veya işlem bu durumda başlatılamaz.");
            state = state with { Operation = operation, Phase = "processing", ExpiresAt = DateTimeOffset.MaxValue, Result = null, Error = null, Fields = fields };
            await SaveAsync(state, ct);
            return state;
        }
        finally { gate.Release(); }
    }

    public async Task CompleteAsync(Guid id, Guid owner, object? result, string? error, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            var state = Read(id, owner);
            state = state with { Phase = error is null ? "completed" : "failed", Error = error,
                Result = result is null ? null : JsonSerializer.SerializeToElement(result, WebJson) };
            await SaveAsync(state, ct);
            // Retain the small result/idempotency receipt, not the now-unused ZIP.
            if (state.Operation == "execute" && error is null) await TryReleaseAsync(state with { ExpiresAt = DateTimeOffset.UtcNow.AddDays(7) }, ct);
        }
        finally { gate.Release(); }
    }

    public async Task DeleteAsync(Guid id, Guid owner, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            var state = Read(id, owner);
            if (state.Phase == "processing" || receiving.ContainsKey(id))
                throw new ZipUploadException("UPLOAD_BUSY", 409, "Aktif aktarım veya sunucu işlemi kapatılamaz. İşlemin bitmesini bekleyin.");
            // Closing never deletes imported data. Keep a terminal receipt to block stale retries.
            if (state.Phase != "completed" || state.Operation != "execute")
                state = state with { Phase = "closed", Result = null, Error = null };
            await SaveAsync(state with { ExpiresAt = DateTimeOffset.UtcNow.AddDays(7) }, ct);
            await ReleaseAsync(state with { ExpiresAt = DateTimeOffset.UtcNow.AddDays(7) }, ct);
        }
        finally { gate.Release(); }
    }

    public async Task MaintainAsync(bool restarting, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(root);
            CleanupExpired();
            foreach (var state in ReadAll())
            {
                if (restarting && state.Phase == "processing")
                    await SaveAsync(state with { Phase = "failed", Error = "Sunucu işlem sırasında yeniden başladı. Mükerrer kayıt riskine karşı otomatik yeniden yürütülmedi; import geçmişini kontrol edin." }, ct);
                else if (!state.ArchiveReleased && (state.Phase == "closed" || state.Phase == "completed" && state.Operation == "execute"))
                    await TryReleaseAsync(state, ct);
            }
        }
        finally { gate.Release(); }
    }

    public async Task<IReadOnlyList<ZipUploadSummary>> ListAsync(Guid owner, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(root);
            CleanupExpired();
            return ReadAll().Where(s => s.Owner == owner && (!s.ArchiveReleased || s.Phase == "processing"))
                .OrderBy(s => s.ExpiresAt)
                .Select(s => new ZipUploadSummary(s.Id, s.Kind, s.FileName, s.Length, s.Offset, s.Phase,
                    s.Operation, s.ExpiresAt, s.Phase != "processing" && !receiving.ContainsKey(s.Id))).ToArray();
        }
        finally { gate.Release(); }
    }

    private void CleanupExpired()
    {
        foreach (var state in ReadAll().Where(s => s.ExpiresAt < DateTimeOffset.UtcNow &&
            !(s.Phase == "completed" && s.Operation is "preview" or "validate") && s.Phase != "processing" && !receiving.ContainsKey(s.Id)))
            TryDeleteExpiredFolder(Folder(state.Id));
        // A crash before metadata was committed can leave an orphan GUID directory.
        // Never delete arbitrary directories or an in-flight session.
        foreach (var folder in Directory.EnumerateDirectories(root))
            if (Guid.TryParseExact(Path.GetFileName(folder), "N", out var id) && !receiving.ContainsKey(id) &&
                !File.Exists(Path.Combine(folder, "state.json")) &&
                Directory.GetLastWriteTimeUtc(folder) < DateTime.UtcNow.AddHours(-24))
                TryDeleteExpiredFolder(folder);
    }

    private void TryDeleteExpiredFolder(string folder)
    {
        try { Directory.Delete(folder, recursive: true); }
        catch (IOException ex) { LogCleanup(ex); }
        catch (UnauthorizedAccessException ex) { LogCleanup(ex); }
    }

    private async Task ReleaseAsync(ZipUploadState state, CancellationToken ct)
    {
        File.Delete(ArchivePath(state.Id));
        await SaveAsync(state with { ArchiveReleased = true, Chunks = [] }, ct);
    }

    private async Task TryReleaseAsync(ZipUploadState state, CancellationToken ct)
    {
        try { await ReleaseAsync(state, ct); }
        catch (IOException ex) { LogCleanup(ex); }
        catch (UnauthorizedAccessException ex) { LogCleanup(ex); }
    }

    private void LogCleanup(Exception exception)
    {
        if (logger is not null) CleanupFailed(logger, exception);
    }

    private static readonly Action<ILogger, Exception?> CleanupFailed = LoggerMessage.Define(
        LogLevel.Warning, new EventId(7502, "ZipUploadCleanupDeferred"), "Temporary ZIP cleanup deferred; reservation retained until cleanup succeeds.");

    private IEnumerable<ZipUploadState> ReadAll() => Directory.EnumerateDirectories(root)
        .Where(folder => Guid.TryParseExact(Path.GetFileName(folder), "N", out _) && File.Exists(Path.Combine(folder, "state.json")))
        .Select(folder => JsonSerializer.Deserialize<ZipUploadState>(File.ReadAllText(Path.Combine(folder, "state.json")))!);

    private ZipUploadState Read(Guid id, Guid owner)
    {
        if (!File.Exists(Metadata(id))) throw new KeyNotFoundException("Yükleme bulunamadı.");
        var state = JsonSerializer.Deserialize<ZipUploadState>(File.ReadAllText(Metadata(id)))!;
        if (state.Owner != owner || state.ExpiresAt < DateTimeOffset.UtcNow && state.Phase != "processing" && !receiving.ContainsKey(id) &&
            !(state.Phase == "completed" && state.Operation is "preview" or "validate"))
            throw new KeyNotFoundException("Yükleme bulunamadı veya süresi doldu.");
        return state;
    }

    private async Task SaveAsync(ZipUploadState state, CancellationToken ct)
    {
        var temp = Metadata(state.Id) + ".tmp";
        await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(state), ct);
        File.Move(temp, Metadata(state.Id), overwrite: true);
    }
}
