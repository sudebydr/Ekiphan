using Ekiphan.Infrastructure.MediaImport;
using System.Security.Cryptography;
using System.Text.Json;
using Ekiphan.Application.MediaImport;
using Ekiphan.Domain.Media;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Ekiphan.Api.Media;

/// <summary>Owner-authorized ZIP receipts survive token expiry/restarts, without creating another batch.</summary>
public sealed class ZipProductMediaSession(ZipUploadStore store, IServiceProvider services)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private IProductMediaImportTokenService Tokens => services.GetRequiredService<IProductMediaImportTokenService>();
    private ProductMediaImportOptions Options => services.GetRequiredService<IOptions<ProductMediaImportOptions>>().Value;
    private string PathFor(Guid id, string name) => Path.Combine(Path.GetDirectoryName(store.ArchivePath(id))!, name + ".json");
    private async Task SaveAsync<T>(Guid id, string name, T value, CancellationToken ct)
    {
        var path = PathFor(id, name);
        await File.WriteAllTextAsync(path + ".tmp", JsonSerializer.Serialize(value, Json), ct);
        File.Move(path + ".tmp", path, true);
    }
    private async Task<T> ReadAsync<T>(Guid id, string name, CancellationToken ct) =>
        JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(PathFor(id, name), ct), Json)!;

    public async Task CapturePreviewAsync(ZipUploadState state, ProductMediaImportPreviewDto preview, CancellationToken ct)
    {
        if (!Tokens.TryGetUpload(preview.UploadToken, out var upload) || upload.UserId != state.Owner)
            throw new ProductMediaImportTokenException("Önizleme oturumu doğrulanamadı.");
        await SaveAsync(state.Id, "product-upload", upload, ct);
        await SaveAsync(state.Id, "product-preview", preview, ct);
    }

    public Task<ProductMediaImportPreviewDto> PreviewAsync(Guid id, CancellationToken ct) =>
        ReadAsync<ProductMediaImportPreviewDto>(id, "product-preview", ct);

    private async Task<ProductMediaStoredUpload> UploadAsync(ZipUploadState state, CancellationToken ct)
    {
        var upload = await ReadAsync<ProductMediaStoredUpload>(state.Id, "product-upload", ct);
        var batch = await services.GetRequiredService<IProductMediaImportRepository>().GetBatchAsync(upload.BatchId, false, ct);
        if (upload.UserId != state.Owner || batch is null || batch.CreatedByUserId != state.Owner ||
            !string.Equals(batch.OriginalFileHash, upload.Archive.Sha256, StringComparison.OrdinalIgnoreCase) ||
            batch.Status is not (ProductMediaImportBatchStatus.Uploaded or ProductMediaImportBatchStatus.Validated))
            throw new ProductMediaImportConflictException("Bu yükleme artık doğrulanamaz veya yeniden içe aktarılamaz. İçe aktarma geçmişini kontrol edin.");
        return upload with { ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(Options.UploadTokenLifetimeMinutes) };
    }

    public async Task<ProductMediaImportValidationResultDto> ValidateAsync(ZipUploadState state, CancellationToken ct)
    {
        var upload = await UploadAsync(state, ct);
        var token = Tokens.CreateUpload(upload);
        var result = await services.GetRequiredService<IProductMediaImportValidationService>()
            .ValidateAsync(new(token, null, new()), ct);
        if (!Tokens.TryGetValidation(result.ValidationToken, out var validation))
            throw new ProductMediaImportTokenException("Doğrulama oturumu kaydedilemedi.");
        await SaveAsync(state.Id, "product-validation", validation, ct);
        return result;
    }

    public async Task<ProductMediaImportExecutionResultDto> ExecuteAsync(ZipUploadState state, Stream archiveStream, CancellationToken ct)
    {
        var upload = await UploadAsync(state, ct);
        var validation = await ReadAsync<ProductMediaStoredValidation>(state.Id, "product-validation", ct);
        if (validation.BatchId != upload.BatchId) throw new ProductMediaImportConflictException("Doğrulama farklı bir yüklemeye ait.");
        var storage = services.GetRequiredService<ITemporaryProductMediaStorage>();
        var needsRestore = false;
        foreach (var file in validation.Files.Where(f => f.Status == ProductMediaValidationFileStatus.Valid && f.MatchedProductId.HasValue))
        {
            try { await using var existing = await storage.OpenReadAsync(upload.Archive.ContainerId, file.TemporaryFileId, ct); }
            catch (FileNotFoundException) { needsRestore = true; break; }
            catch (DirectoryNotFoundException) { needsRestore = true; break; }
        }
        if (!needsRestore) return await ExecuteRenewedAsync(upload, validation, ct);
        // The retained ZIP is authoritative; extracted preview files may have expired.
        // Rehydrate only importable entries, preserving batch item IDs, with bounded streaming.
        archiveStream.Position = 0;
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(archiveStream, ct));
        if (!hash.Equals(upload.Archive.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new ProductMediaImportSecurityException("Saklanan ZIP doğrulama sonrasında değişmiş.");
        archiveStream.Position = 0;
        var container = Guid.NewGuid().ToString("N");
        try
        {
            using var archive = ImportArchive.Open(archiveStream, state.FileName, Options.MaxFileCount, Options.MaxExtractedBytes, Options.MaxCompressionRatio);
            var entriesById = upload.Archive.Entries.Select((entry, index) => (entry, index))
                .ToDictionary(x => x.entry.TemporaryFileId, StringComparer.Ordinal);
            foreach (var file in validation.Files.Where(f => f.Status == ProductMediaValidationFileStatus.Valid && f.MatchedProductId.HasValue)
                .OrderBy(f => entriesById[f.TemporaryFileId].index))
            {
                var (expected, index) = entriesById[file.TemporaryFileId];
                var entry = archive.Entries[index];
                if (entry.FullName != expected.OriginalFileName) throw new ProductMediaImportSecurityException("ZIP girdi sırası uyuşmuyor.");
                if (entry.Length != expected.Length || entry.Length > Options.MaxSingleImageBytes)
                    throw new ProductMediaImportSecurityException("Saklanan görsel boyutu uyuşmuyor.");
                await using var content = entry.Open();
                await storage.StoreFileAsync(container, file.TemporaryFileId, content, ct);
            }
            return await ExecuteRenewedAsync(upload with { Archive = upload.Archive with { ContainerId = container } }, validation, ct);
        }
        finally
        {
            try { await storage.DeleteAsync(container, CancellationToken.None); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                var logger = services.GetService<ILogger<ZipProductMediaSession>>();
                if (logger is not null) CleanupDeferred(logger, state.Id, ex);
            }
        }
    }

    private static readonly Action<ILogger, Guid, Exception?> CleanupDeferred = LoggerMessage.Define<Guid>(
        LogLevel.Warning, new EventId(7503, "ZipExtractedCleanupDeferred"),
        "Extracted ZIP cleanup deferred. UploadId={UploadId}");

    private Task<ProductMediaImportExecutionResultDto> ExecuteRenewedAsync(ProductMediaStoredUpload upload,
        ProductMediaStoredValidation validation, CancellationToken ct)
    {
        var token = Tokens.CreateUpload(upload);
        var renewed = Tokens.CreateValidation(validation with { UploadToken = token, Used = false,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(Options.ValidationTokenLifetimeMinutes) });
        return services.GetRequiredService<IProductMediaImportExecutionService>().ExecuteAsync(new(renewed), ct);
    }
}
