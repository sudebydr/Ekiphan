using System.Collections.Concurrent;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Ekiphan.Application.Media;
using Ekiphan.Application.MediaImport;
using Ekiphan.Domain.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

#pragma warning disable CA1848

namespace Ekiphan.Infrastructure.MediaImport;

public sealed class ProductMediaSkuParser : IProductMediaSkuParser
{
    private static readonly Regex NumberSuffix = new(@"(?<sep>_|-)(?<n>[1-9]\d*)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex PositionSuffix = new(@"(?:_|-)(?<p>main|detail|front|back|side)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public ProductMediaSkuParseResult Parse(string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName).Trim();
        if (string.IsNullOrWhiteSpace(stem)) return new(null, ProductMediaDetectedPosition.Unknown, 0, false);
        var position = ProductMediaDetectedPosition.Gallery;
        var order = 100;
        var primary = false;
        var positionMatch = PositionSuffix.Match(stem);
        if (positionMatch.Success)
        {
            position = positionMatch.Groups["p"].Value.ToLowerInvariant() switch
            { "main" => ProductMediaDetectedPosition.Main, "detail" => ProductMediaDetectedPosition.Detail,
              "front" => ProductMediaDetectedPosition.Front, "back" => ProductMediaDetectedPosition.Back,
              "side" => ProductMediaDetectedPosition.Side, _ => ProductMediaDetectedPosition.Unknown };
            primary = position == ProductMediaDetectedPosition.Main;
            order = position switch { ProductMediaDetectedPosition.Main => 1, ProductMediaDetectedPosition.Front => 20,
                ProductMediaDetectedPosition.Side => 30, ProductMediaDetectedPosition.Back => 40,
                ProductMediaDetectedPosition.Detail => 50, _ => 100 };
            stem = stem[..positionMatch.Index];
        }
        else
        {
            var numberMatch = NumberSuffix.Match(stem);
            if (numberMatch.Success &&
                int.TryParse(numberMatch.Groups["n"].Value, out var number))
            { order = number; primary = number == 1; stem = stem[..numberMatch.Index]; }
        }

        stem = stem.Trim(' ', '-', '_');
        return string.IsNullOrWhiteSpace(stem)
            ? new(null, ProductMediaDetectedPosition.Unknown, order, primary)
            : new(stem.ToUpperInvariant(), position, order, primary);
    }
}

public sealed class Sha256ProductMediaDuplicateDetector : IProductMediaDuplicateDetector
{ public string ComputeHash(ReadOnlySpan<byte> content) => Convert.ToHexString(SHA256.HashData(content)); }

public sealed class ProductMediaImportTokenService(TimeProvider clock)
    : IProductMediaImportTokenService
{
    private readonly ConcurrentDictionary<string, ProductMediaStoredUpload> uploads = new();
    private readonly ConcurrentDictionary<string, ProductMediaStoredValidation> validations = new();
    public string CreateUpload(ProductMediaStoredUpload upload) { Purge(); var token = Token(); uploads[token] = upload; return token; }
    public bool TryGetUpload(string token, out ProductMediaStoredUpload upload)
    {
        Purge(); if (uploads.TryGetValue(token, out upload!) && upload.ExpiresAt > clock.GetUtcNow()) return true;
        uploads.TryRemove(token, out _); upload = null!; return false;
    }
    public string CreateValidation(ProductMediaStoredValidation validation) { Purge(); var token = Token(); validations[token] = validation; return token; }
    public bool TryUseValidation(string token, out ProductMediaStoredValidation validation)
    {
        Purge();
        while (validations.TryGetValue(token, out validation!))
        {
            if (validation.Used || validation.ExpiresAt <= clock.GetUtcNow()) { validations.TryRemove(token, out _); break; }
            if (validations.TryUpdate(token, validation with { Used = true }, validation)) return true;
        }
        validation = null!; return false;
    }
    private void Purge()
    {
        var now = clock.GetUtcNow();
        foreach (var x in uploads.Where(x => x.Value.ExpiresAt <= now)) uploads.TryRemove(x.Key, out _);
        foreach (var x in validations.Where(x => x.Value.ExpiresAt <= now || x.Value.Used)) validations.TryRemove(x.Key, out _);
    }
    private static string Token() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}

internal sealed class TemporaryProductMediaStorage(IOptions<ProductMediaImportOptions> options, IConfiguration configuration, TimeProvider clock)
    : ITemporaryProductMediaStorage
{
    private readonly string root = ResolveRoot(configuration["ProductMediaImport:TemporaryRoot"]);
    public async Task StoreFileAsync(string containerId, string temporaryFileId, Stream content, CancellationToken cancellationToken = default)
    {
        CleanupExpired(); var path = Resolve(containerId, temporaryFileId); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        await content.CopyToAsync(output, cancellationToken);
    }
    public Task<Stream> OpenReadAsync(string containerId, string temporaryFileId, CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult<Stream>(new FileStream(Resolve(containerId, temporaryFileId), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan)); }
    public Task DeleteAsync(string containerId, CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); var dir = ResolveContainer(containerId); if (Directory.Exists(dir)) Directory.Delete(dir, true); return Task.CompletedTask; }
    private void CleanupExpired()
    {
        Directory.CreateDirectory(root); var cutoff = clock.GetUtcNow().UtcDateTime.AddMinutes(-options.Value.TemporaryFileLifetimeMinutes);
        foreach (var dir in Directory.EnumerateDirectories(root)) if (Directory.GetLastWriteTimeUtc(dir) < cutoff) { try { Directory.Delete(dir, true); } catch (IOException) { } }
    }
    private string Resolve(string container, string file) => Path.Combine(ResolveContainer(container), Safe(file));
    private string ResolveContainer(string container) { var path = Path.GetFullPath(Path.Combine(root, Safe(container))); if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new ProductMediaImportSecurityException("Unsafe temporary path."); return path; }
    private static string Safe(string value) => Regex.IsMatch(value, "^[A-Za-z0-9_-]{1,100}$") ? value : throw new ProductMediaImportSecurityException("Unsafe temporary identifier.");
    private static string ResolveRoot(string? configured) { var path = Path.GetFullPath(string.IsNullOrWhiteSpace(configured) ? Path.Combine(Path.GetTempPath(), "ekiphan-media-import") : configured); Directory.CreateDirectory(path); return path.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar; }
}

public sealed class ZipProductMediaImportArchiveReader(
    IOptions<ProductMediaImportOptions> options,
    ITemporaryProductMediaStorage temporaryStorage,
    IProductMediaSkuParser skuParser,
    IProductMediaDuplicateDetector duplicateDetector,
    IMediaFileSignatureValidator signatureValidator,
    ILogger<ZipProductMediaImportArchiveReader> logger) : IProductMediaImportArchiveReader
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
    public async Task<ProductMediaArchiveReadResult> ReadAsync(string containerId, Stream content, string fileName,
        string contentType, long length, CancellationToken cancellationToken = default)
    {
        var limits = options.Value;
        if (!Path.GetExtension(fileName).Equals(".zip", StringComparison.OrdinalIgnoreCase) ||
            contentType is not ("application/zip" or "application/x-zip-compressed"))
            throw new ProductMediaImportSecurityException("Only ZIP files are accepted.");
        if (length <= 0 || length > limits.MaxZipBytes) throw new ProductMediaImportSecurityException("ZIP size limit exceeded.");
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Product media ZIP upload started. FileName={FileName}", Path.GetFileName(fileName));
        await temporaryStorage.StoreFileAsync(containerId, "archive", content, cancellationToken);
        await using var zipStream = await temporaryStorage.OpenReadAsync(containerId, "archive", cancellationToken);
        if (!HasZipSignature(zipStream)) throw new ProductMediaImportSecurityException("ZIP signature is invalid.");
        if (HasEncryptedEntries(zipStream)) throw new ProductMediaImportSecurityException("Encrypted ZIP files are not allowed.");
        zipStream.Position = 0;
        var zipHash = Convert.ToHexString(await SHA256.HashDataAsync(zipStream, cancellationToken));
        zipStream.Position = 0;
        var result = new List<ProductMediaArchiveEntry>();
        long extracted = 0; int total = 0;
        try
        {
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
            if (archive.Entries.Count > limits.MaxFileCount) throw new ProductMediaImportSecurityException("ZIP entry count limit exceeded.");
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested(); total++;
                ValidateEntryPath(entry.FullName);
                if (string.IsNullOrEmpty(entry.Name) || IsHidden(entry.FullName)) continue;
                var extension = Path.GetExtension(entry.Name).ToLowerInvariant();
                if (entry.Name.Length > 260 || entry.FullName.Length > 500)
                { result.Add(InvalidEntry(entry, extension, ProductMediaPreviewFileStatus.SecurityRejected, "FILE_NAME_TOO_LONG", "The ZIP entry name is too long.")); continue; }
                if (extension == ".zip") throw new ProductMediaImportSecurityException("Nested ZIP files are not allowed.");
                if (!Extensions.Contains(extension))
                {
                    result.Add(InvalidEntry(entry, extension, ProductMediaPreviewFileStatus.Unsupported, "UNSUPPORTED_TYPE", "The file type is not supported.")); continue;
                }
                if (entry.Length <= 0 || entry.Length > Math.Min(limits.MaxSingleImageBytes, 20L * 1024 * 1024))
                { result.Add(InvalidEntry(entry, extension, ProductMediaPreviewFileStatus.Invalid, "IMAGE_SIZE", "Image size limit exceeded.")); continue; }
                extracted = checked(extracted + entry.Length);
                if (extracted > limits.MaxExtractedBytes) throw new ProductMediaImportSecurityException("Extracted size limit exceeded.");
                if (entry.CompressedLength == 0 || entry.Length / Math.Max(1d, entry.CompressedLength) > limits.MaxCompressionRatio)
                    throw new ProductMediaImportSecurityException("Suspicious ZIP compression ratio detected.");
                var id = Guid.NewGuid().ToString("N");
                await using var source = entry.Open();
                await using var buffer = new MemoryStream((int)entry.Length);
                await source.CopyToAsync(buffer, cancellationToken);
                if (buffer.Length != entry.Length) throw new ProductMediaImportSecurityException("ZIP entry length changed while reading.");
                var mime = Mime(extension); buffer.Position = 0;
                if (!signatureValidator.IsValid(buffer, MediaAssetType.Image, mime, entry.Name) || !TryReadDimensions(buffer, extension, out var width, out var height) || (long)width * height > 100_000_000)
                { result.Add(InvalidEntry(entry, extension, ProductMediaPreviewFileStatus.Invalid, "INVALID_IMAGE", "Image signature or dimensions are invalid.")); continue; }
                var bytes = buffer.ToArray(); var hash = duplicateDetector.ComputeHash(bytes); buffer.Position = 0;
                await temporaryStorage.StoreFileAsync(containerId, id, buffer, cancellationToken);
                var parsed = skuParser.Parse(entry.Name);
                result.Add(new(id, entry.FullName, Path.GetFileName(entry.Name), extension, mime, entry.Length,
                    entry.CompressedLength, hash, parsed, parsed.Sku is null ? ProductMediaPreviewFileStatus.MissingSku : ProductMediaPreviewFileStatus.Ready,
                    parsed.Sku is null ? "MISSING_SKU" : null, parsed.Sku is null ? "SKU could not be extracted from the file name." : null));
            }
        }
        catch (InvalidDataException ex) { logger.LogWarning(ex, "Product media ZIP security validation failed. FileName={FileName}", Path.GetFileName(fileName)); throw new ProductMediaImportSecurityException("ZIP is invalid, encrypted, or uses an unsupported compression method."); }
        var duplicates = result.Where(x => x.Status == ProductMediaPreviewFileStatus.Ready).GroupBy(x => x.Sha256).Where(g => g.Count() > 1).SelectMany(g => g.Skip(1)).Select(x => x.TemporaryFileId).ToHashSet();
        result = result.Select(x => duplicates.Contains(x.TemporaryFileId) ? x with { Status = ProductMediaPreviewFileStatus.Duplicate, ErrorCode = "DUPLICATE_CONTENT", ErrorMessage = "Duplicate content in the ZIP." } : x).ToList();
        return new(containerId, Path.GetFileName(fileName), length, zipHash, total, result);
    }

    private static ProductMediaArchiveEntry InvalidEntry(ZipArchiveEntry e, string ext, ProductMediaPreviewFileStatus status, string code, string message) =>
        new(Guid.NewGuid().ToString("N"), e.FullName, Path.GetFileName(e.Name), ext, "application/octet-stream", e.Length, e.CompressedLength, string.Empty,
            new(null, ProductMediaDetectedPosition.Unknown, 0, false), status, code, message);
    private static bool HasZipSignature(Stream s) { Span<byte> b = stackalloc byte[4]; s.Position = 0; return s.Read(b) == 4 && b[0] == 0x50 && b[1] == 0x4B && b[2] is 0x03 or 0x05 or 0x07 && b[3] is 0x04 or 0x06 or 0x08; }
    private static bool HasEncryptedEntries(Stream stream)
    {
        if (!stream.CanSeek || stream.Length < 22) return true;
        var tailLength = (int)Math.Min(stream.Length, 65_557); var tail = new byte[tailLength];
        stream.Position = stream.Length - tailLength; stream.ReadExactly(tail);
        var eocd = -1;
        for (var i = tail.Length - 22; i >= 0; i--) if (BitConverter.ToUInt32(tail, i) == 0x06054B50) { eocd = i; break; }
        if (eocd < 0) return true;
        var count = BitConverter.ToUInt16(tail, eocd + 10); var offset = BitConverter.ToUInt32(tail, eocd + 16);
        stream.Position = offset; using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true);
        for (var n = 0; n < count; n++)
        {
            if (reader.ReadUInt32() != 0x02014B50) return true;
            reader.ReadUInt16(); reader.ReadUInt16(); var flags = reader.ReadUInt16();
            if ((flags & 1) != 0) return true;
            stream.Position += 18;
            var nameLength = reader.ReadUInt16(); var extraLength = reader.ReadUInt16(); var commentLength = reader.ReadUInt16();
            stream.Position += 12 + nameLength + extraLength + commentLength;
        }
        stream.Position = 0; return false;
    }
    private static bool IsHidden(string path) => path.Split('/', '\\').Any(x => x.StartsWith('.') || x.Equals("__MACOSX", StringComparison.OrdinalIgnoreCase) || x.Equals("Thumbs.db", StringComparison.OrdinalIgnoreCase));
    private static void ValidateEntryPath(string path)
    { if (Path.IsPathRooted(path) || path.Contains("..", StringComparison.Ordinal) || Regex.IsMatch(path, "^[A-Za-z]:") || path.Contains('\\')) throw new ProductMediaImportSecurityException("Unsafe ZIP entry path detected."); }
    private static string Mime(string ext) => ext switch { ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", _ => "application/octet-stream" };
    private static bool TryReadDimensions(Stream stream, string ext, out int width, out int height)
    {
        width = height = 0; stream.Position = 0; using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true);
        try
        {
            if (ext == ".png") { stream.Position = 16; width = ReadBig(reader); height = ReadBig(reader); return width > 0 && height > 0; }
            if (ext == ".webp")
            {
                stream.Position = 12; var kind = new string(reader.ReadChars(4));
                if (kind == "VP8X") { stream.Position = 24; width = Read24(reader) + 1; height = Read24(reader) + 1; return width > 0 && height > 0; }
                if (kind == "VP8L") { stream.Position = 20; if (reader.ReadByte() != 0x2F) return false; var bits = reader.ReadUInt32(); width = (int)(bits & 0x3FFF) + 1; height = (int)((bits >> 14) & 0x3FFF) + 1; return true; }
                if (kind == "VP8 ") { stream.Position = 23; if (reader.ReadByte() != 0x9D || reader.ReadByte() != 0x01 || reader.ReadByte() != 0x2A) return false; width = reader.ReadUInt16() & 0x3FFF; height = reader.ReadUInt16() & 0x3FFF; return width > 0 && height > 0; }
                return false;
            }
            stream.Position = 2;
            while (stream.Position + 9 < stream.Length) { if (reader.ReadByte() != 0xFF) continue; var marker = reader.ReadByte(); var len = (reader.ReadByte() << 8) + reader.ReadByte(); if (len < 2) return false; if (marker is >= 0xC0 and <= 0xC3) { reader.ReadByte(); height = (reader.ReadByte() << 8) + reader.ReadByte(); width = (reader.ReadByte() << 8) + reader.ReadByte(); return width > 0 && height > 0; } stream.Position += len - 2; }
        } catch (EndOfStreamException) { }
        return false;
    }
    private static int ReadBig(BinaryReader r) => (r.ReadByte() << 24) | (r.ReadByte() << 16) | (r.ReadByte() << 8) | r.ReadByte();
    private static int Read24(BinaryReader r) => r.ReadByte() | (r.ReadByte() << 8) | (r.ReadByte() << 16);
}
