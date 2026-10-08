using System.IO.Compression;
using System.Text;
using System.Globalization;
using Ekiphan.Application.CatalogPdfImport;
using Ekiphan.Domain.Media;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Ekiphan.Application.Media;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Ekiphan.Infrastructure.CatalogPdfImport;

public sealed class CatalogPdfImportService(EkiphanDbContext db, IMediaFileStorage storage, IPdfCoverRenderer? coverRenderer = null, ILogger<CatalogPdfImportService>? logger = null) : ICatalogPdfImportService
{
    // Known names are normalized centrally; unknown catalog names remain dynamic.
    public static readonly IReadOnlyList<CatalogPdfManifestItem> Manifest =
    [
        new("acik-bufe", "Ekiphan açık büfe", "Ekiphan-açık büfe.pdf"),
        new("bar", "Ekiphan bar", "Ekiphan-bar.pdf"),
        new("cihazlar", "Ekiphan cihazlar", "Ekiphan-cihazlar.pdf"),
        new("restaurant", "Restaurant malzemeleri", "Ekiphan_Restaurant Malzemeleri.pdf"),
        new("klasik", "Klasik seçki", "Ekiphan_catal_kasik_2026.pdf"),
        new("kutahya", "Kütahya Porselen", "Kütahya-Porselen-2026.pdf"),
        new("bonna", "Bonna", "bonna-katalog-2026.pdf"),
        new("esma", "Esmadereboy", "esmadereboy-2026.pdf"),
        new("fabrika", "Fabrika", "fabrika-2026.pdf"),
        new("nude", "Nude", "nude-2026.pdf"),
        new("pasabahce", "Paşabahçe", "paşabahçe-2026.pdf"),
        new("selene", "Selene", "selene_11.06.26.pdf")
    ];

    public async Task<CatalogPdfPreview> PreviewAsync(Stream content, string fileName, long length, CancellationToken cancellationToken = default)
    {
        if (!Path.GetExtension(fileName).Equals(".zip", StringComparison.OrdinalIgnoreCase) || length is <= 0 or > 1536L * 1024 * 1024)
            throw new ArgumentException("A ZIP file up to 1.5 GB is required.");

        using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
        if (archive.Entries.Count > 100) throw new ArgumentException("ZIP contains too many entries.");
        var files = new List<CatalogPdfPreviewFile>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(entry.Name)) continue;
            if (entry.FullName.StartsWith("__MACOSX/", StringComparison.Ordinal) || entry.Name.StartsWith("._", StringComparison.Ordinal))
            {
                files.Add(new(entry.FullName, entry.Name, entry.Length, null, Title(entry.Name), "Ignored", null));
                continue;
            }
            if (Path.IsPathRooted(entry.FullName) || entry.FullName.Contains("..", StringComparison.Ordinal) || entry.FullName.Contains('\\'))
                throw new ArgumentException("ZIP contains an unsafe entry path.");
            if (!Path.GetExtension(entry.Name).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                files.Add(new(entry.FullName, entry.Name, entry.Length, null, Title(entry.Name), "Rejected", "Only PDF files are allowed."));
                continue;
            }
            var normalized = Normalize(entry.Name);
            if (!names.Add(normalized)) throw new ArgumentException("ZIP contains duplicate normalized PDF filenames.");
            if (entry.Length is <= 0 or > 500L * 1024 * 1024) throw new ArgumentException("A PDF exceeds the 500 MB limit.");
            await using var pdf = entry.Open();
            var signature = new byte[5];
            if (await pdf.ReadAsync(signature, cancellationToken) != signature.Length || !signature.AsSpan().SequenceEqual("%PDF-"u8))
                throw new ArgumentException($"'{entry.Name}' is not a valid PDF.");
            var slug = Slug(entry.Name);
            files.Add(new(entry.FullName, entry.Name, entry.Length, slug, Title(entry.Name), "Matched", null));
        }
        if (files.Any(file => file.Status == "Rejected")) throw new ArgumentException("ZIP may contain only valid PDF files.");
        return new(files.Count(file => file.Status == "Matched"), files.Count(file => file.Status == "Ignored"), files);
    }

    public async Task<IReadOnlyList<CatalogPdfDocument>> GetPublicDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var assets = await db.MediaAssets.AsNoTracking()
            .Where(asset => asset.AssetType == MediaAssetType.Pdf && asset.Status == MediaStatus.Active &&
                asset.StorageKey != null && asset.StorageKey.StartsWith("catalogs/"))
            .OrderByDescending(asset => asset.CreatedAt)
            .Select(asset => new { asset.Id, asset.CoverMediaAssetId, asset.StorageKey, asset.OriginalFileName, asset.CreatedAt,
                Title = asset.Translations.Where(t => t.LanguageCode == "tr").Select(t => t.Title).FirstOrDefault() })
            .ToListAsync(cancellationToken);
        var coverIds = assets.Where(x => x.CoverMediaAssetId.HasValue).Select(x => x.CoverMediaAssetId!.Value).ToArray();
        var covers = await db.MediaAssets.AsNoTracking()
            .Where(asset => asset.AssetType == MediaAssetType.Image && asset.Status == MediaStatus.Active &&
                asset.StorageKey != null && (coverIds.Contains(asset.Id) || (asset.StorageKey.StartsWith("catalogs/") && asset.StorageKey.EndsWith("-cover.webp"))))
            .Select(asset => new { asset.Id, asset.StorageKey }).ToListAsync(cancellationToken);
        var coverKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var cover in covers)
        {
            await using var file = await storage.OpenReadAsync(cover.StorageKey!, cancellationToken);
            if (file is not null) coverKeys.Add(cover.StorageKey!);
        }
        string? Key(Guid? coverId, string pdfKey) => coverId.HasValue
            ? covers.FirstOrDefault(x => x.Id == coverId)?.StorageKey
            : CoverKeyForPdf(pdfKey);
        return assets.Select(asset => new CatalogPdfDocument(asset.Id,
            asset.StorageKey!.Split('/').Skip(1).FirstOrDefault() ?? asset.Id.ToString("N"),
            asset.Title ?? asset.OriginalFileName!, asset.OriginalFileName!,
            $"/media/{asset.StorageKey}",
            Key(asset.CoverMediaAssetId, asset.StorageKey) is { } coverKey && coverKeys.Contains(coverKey)
                ? $"/media/{coverKey}"
                : "/images/catalog-placeholder.webp", asset.CreatedAt)).ToArray();
    }

    public async Task<CatalogPdfCoverBackfillResult> BackfillCoversAsync(CancellationToken cancellationToken = default)
    {
        var documents = await db.MediaAssets.Include(asset => asset.Translations)
            .Where(asset => asset.AssetType == MediaAssetType.Pdf && asset.Status == MediaStatus.Active &&
                asset.StorageKey != null && asset.StorageKey.StartsWith("catalogs/"))
            .ToListAsync(cancellationToken);
        var created = 0; var skipped = 0; var failed = 0;
        foreach (var document in documents)
        {
            var slug = document.StorageKey!.Split('/').Skip(1).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(slug)) { failed++; continue; }
            string? generatedKey = null;
            try
            {
                if (db.Entry(document).State == EntityState.Detached) db.Attach(document);
                var generated = await EnsureCoverAsync(document, document.Translations.FirstOrDefault(x => x.LanguageCode == "tr")?.Title ?? slug, cancellationToken);
                if (generated) generatedKey = (await CoverUrlAsync(document, cancellationToken))["/media/".Length..];
                await db.SaveChangesAsync(cancellationToken);
                if (generated) created++; else skipped++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failed++;
                logger?.LogWarning(exception, "Catalog cover backfill failed for {StorageKey}", document.StorageKey);
                db.ChangeTracker.Clear();
                if (generatedKey is not null) await storage.DeleteAsync(generatedKey, cancellationToken);
            }
        }
        return new(created, skipped, failed);
    }

    public async Task<CatalogPdfExecutionResult> ExecuteAsync(Stream content, string fileName, long length, IReadOnlyDictionary<string, string>? titles = null, CancellationToken cancellationToken = default)
    {
        var preview = await PreviewAsync(content, fileName, length, cancellationToken);
        if (preview.Files.Any(file => file.Status is not "Matched" and not "Ignored"))
            throw new ArgumentException("ZIP contains an invalid catalog file.");
        if (!content.CanSeek) throw new ArgumentException("Catalog ZIP stream must be seekable.");
        content.Position = 0;
        using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
        var items = preview.Files.Where(item => item.Status == "Matched").ToArray();
        var keys = items.Select(item => StorageKey(item.CatalogSlug!)).ToArray();
        var existingKeys = (await db.MediaAssets
            .Where(asset => asset.StorageKey != null && keys.Contains(asset.StorageKey))
            .ToListAsync(cancellationToken)).ToDictionary(asset => asset.StorageKey!, StringComparer.Ordinal);
        var savedKeys = new List<string>();
        var created = 0;
        var skipped = 0;
        try
        {
            foreach (var item in items)
            {
                var entry = archive.Entries.Single(entry => entry.FullName == item.EntryPath);
                var key = StorageKey(item.CatalogSlug!);
                if (existingKeys.TryGetValue(key, out var existingAsset))
                {
                    await using var storedFile = await storage.OpenReadAsync(key, cancellationToken);
                    if (storedFile is not null)
                    {
                        skipped++;
                    }
                    else
                    {
                        await using var replacement = entry.Open();
                        await storage.SaveAsync(key, replacement, entry.Length, cancellationToken);
                        savedKeys.Add(key);
                    }
                    try
                    {
                        if (await EnsureCoverAsync(existingAsset, item.SuggestedTitle, cancellationToken))
                            savedKeys.Add(CoverKeyForPdf(key));
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        logger?.LogWarning(exception, "Catalog cover recovery failed for {StorageKey}", key);
                    }
                    continue;
                }

                await using (var existingObject = await storage.OpenReadAsync(key, cancellationToken))
                {
                    if (existingObject is not null)
                        throw new InvalidOperationException($"Catalog storage key '{key}' already exists without a media asset.");
                }
                await using var hashing = entry.Open();
                var hash = Convert.ToHexString(await SHA256.HashDataAsync(hashing, cancellationToken));
                await using var upload = entry.Open();
                await storage.SaveAsync(key, upload, entry.Length, cancellationToken);
                savedKeys.Add(key);
                var asset = MediaAsset.CreateFile(Guid.NewGuid(), MediaAssetType.Pdf, item.FileName, key,
                    "application/pdf", entry.Length, hash, storage.ProviderName);
                var chosenTitle = titles?.GetValueOrDefault(item.EntryPath)?.Trim();
                asset.AddTranslation("tr", string.IsNullOrWhiteSpace(chosenTitle) ? item.SuggestedTitle : chosenTitle);
                db.MediaAssets.Add(asset);
                try
                {
                    if (await EnsureCoverAsync(asset, string.IsNullOrWhiteSpace(chosenTitle) ? item.SuggestedTitle : chosenTitle, cancellationToken))
                        savedKeys.Add(CoverKeyForPdf(key));
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger?.LogWarning(exception, "Catalog cover rendering failed for {StorageKey}", key);
                    // The PDF remains usable; the public catalog falls back to its placeholder cover.
                }
                created++;
            }
            await db.SaveChangesAsync(cancellationToken);
            return new(savedKeys.Count, created, skipped, 0);
        }
        catch
        {
            foreach (var key in savedKeys) await storage.DeleteAsync(key, CancellationToken.None);
            throw;
        }
    }

    public static string StorageKey(string slug) => $"catalogs/{slug}/{slug}.pdf";
    public static string CoverStorageKey(string slug) => $"catalogs/{slug}/{slug}-cover.webp";
    public static string Title(string fileName)
    {
        var known = Manifest.SingleOrDefault(item => Normalize(item.FileName) == Normalize(fileName));
        if (known is not null) return known.Title;
        var value = Path.GetFileNameWithoutExtension(fileName).Replace('_', ' ').Replace('-', ' ');
        value = System.Text.RegularExpressions.Regex.Replace(value, @"(?:\s+|[_.-])(?:20)?\d{2}(?:[.\-]\d{1,2}){0,2}$", "", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        value = System.Text.RegularExpressions.Regex.Replace(value, @"\s+", " ").Trim();
        return System.Globalization.CultureInfo.GetCultureInfo("tr-TR").TextInfo.ToTitleCase(value.ToLowerInvariant());
    }

    public async Task<SingleCatalogPdfResult> UploadSingleAsync(Stream content, string fileName, long length, string? title,
        CancellationToken cancellationToken = default)
    {
        if (!Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only PDF files are allowed.");
        if (length is <= 0 or > 500L * 1024 * 1024)
            throw new ArgumentException("A PDF file up to 500 MB is required.");
        if (!content.CanSeek) throw new ArgumentException("Catalog PDF stream must be seekable.");
        var signature = new byte[5];
        if (await content.ReadAsync(signature, cancellationToken) != signature.Length || !signature.AsSpan().SequenceEqual("%PDF-"u8))
            throw new ArgumentException("The selected file is not a valid PDF.");
        content.Position = 0;

        var catalogTitle = string.IsNullOrWhiteSpace(title) ? Title(fileName) : title.Trim();
        if (catalogTitle.Length > 250) throw new ArgumentException("Catalog title cannot exceed 250 characters.");
        var baseSlug = Slug(fileName);
        var slug = baseSlug;
        for (var suffix = 2; await db.MediaAssets.AnyAsync(x => x.StorageKey == StorageKey(slug), cancellationToken); suffix++)
            slug = $"{baseSlug}-{suffix}";
        var pdfKey = StorageKey(slug);
        var coverKey = CoverStorageKey(slug);
        var savedKeys = new List<string>();
        try
        {
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(content, cancellationToken));
            content.Position = 0;
            await storage.SaveAsync(pdfKey, content, length, cancellationToken);
            savedKeys.Add(pdfKey);
            var asset = MediaAsset.CreateFile(Guid.NewGuid(), MediaAssetType.Pdf, fileName, pdfKey,
                "application/pdf", length, hash, storage.ProviderName);
            asset.AddTranslation("tr", catalogTitle);
            db.MediaAssets.Add(asset);

            string? warning = null;
            try
            {
                if (await EnsureCoverAsync(asset, catalogTitle, cancellationToken)) savedKeys.Add(coverKey);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger?.LogWarning(exception, "Catalog cover rendering failed for {StorageKey}", pdfKey);
                warning = "PDF yüklendi ancak kapak üretilemedi; placeholder kullanılacak.";
            }

            await db.SaveChangesAsync(cancellationToken);
            return new(asset.Id, catalogTitle, fileName, $"/media/{pdfKey}",
                await CoverUrlAsync(asset, cancellationToken), warning);
        }
        catch
        {
            foreach (var key in savedKeys) await storage.DeleteAsync(key, CancellationToken.None);
            throw;
        }
    }

    private static string CoverKeyForPdf(string pdfKey) => pdfKey[..^4] + "-cover.webp";

    private async Task<string> CoverUrlAsync(MediaAsset pdf, CancellationToken ct)
    {
        var cover = db.MediaAssets.Local.FirstOrDefault(x => x.Id == pdf.CoverMediaAssetId)
            ?? await db.MediaAssets.SingleOrDefaultAsync(x => x.Id == pdf.CoverMediaAssetId, ct);
        return cover is null ? "/images/catalog-placeholder.webp" : $"/media/{cover.StorageKey}";
    }

    private async Task<bool> EnsureCoverAsync(MediaAsset document, string title, CancellationToken ct)
    {
        var pdfKey = document.StorageKey!;
        var key = CoverKeyForPdf(pdfKey);
        var asset = await db.MediaAssets.SingleOrDefaultAsync(x => x.Id == document.CoverMediaAssetId || x.StorageKey == key, ct);
        if (asset is not null) key = asset.StorageKey!;
        await using (var existing = await storage.OpenReadAsync(key, ct))
        {
            if (existing is not null && asset is not null) { document.SetCover(asset.Id); return false; }
            if (existing is not null) throw new InvalidOperationException("Cover file exists without a media record.");
        }
        await using var pdf = await storage.OpenReadAsync(pdfKey, ct)
            ?? throw new FileNotFoundException("Catalog PDF file is missing.");
        await using var cover = new MemoryStream();
        if (coverRenderer is null) RenderCover(cover, pdf); else coverRenderer.Render(cover, pdf);
        cover.Position = 0;
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(cover, ct));
        {
            var assetId = asset?.Id;
            var reusable = db.MediaAssets.Local.FirstOrDefault(x => x.Id != assetId && x.AssetType == MediaAssetType.Image && x.Status == MediaStatus.Active && x.Sha256Checksum == hash)
                ?? await db.MediaAssets.FirstOrDefaultAsync(x => x.Id != assetId && x.AssetType == MediaAssetType.Image && x.Status == MediaStatus.Active && x.Sha256Checksum == hash, ct);
            if (reusable is not null)
            {
                await using var binary = await storage.OpenReadAsync(reusable.StorageKey!, ct);
                if (binary is not null) { document.SetCover(reusable.Id); return false; }
                throw new IOException("Matching cover asset binary is missing.");
            }
        }
        // Validate before writing; an existing cover record is repaired, not duplicated.
        var generated = MediaAsset.CreateFile(Guid.NewGuid(), MediaAssetType.Image, Path.GetFileName(key), key,
            "image/webp", cover.Length, hash, storage.ProviderName);
        if (asset is null)
        {
            generated.AddTranslation("tr", $"{title} kapak", $"{title} katalog kapağı");
        }
        cover.Position = 0;
        await storage.SaveAsync(key, cover, cover.Length, ct);
        if (asset is null) db.MediaAssets.Add(generated);
        else asset.RepairImageFile(cover.Length, hash);
        document.SetCover((asset ?? generated).Id);
        return true;
    }

    public async Task<SingleCatalogPdfResult> ReplaceAsync(Guid id, Stream content, string fileName, long length,
        string? title, CancellationToken cancellationToken = default)
    {
        var asset = await db.MediaAssets.Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (asset is null || asset.AssetType != MediaAssetType.Pdf || asset.StorageKey?.StartsWith("catalogs/", StringComparison.Ordinal) != true)
            throw new ArgumentException("Katalog PDF kaydı bulunamadı.");
        if (!content.CanSeek || !Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase) || length is <= 0 or > 500L * 1024 * 1024)
            throw new ArgumentException("En fazla 500 MB PDF dosyası gereklidir.");
        var signature = new byte[5];
        content.Position = 0;
        if (await content.ReadAsync(signature, cancellationToken) != 5 || !signature.AsSpan().SequenceEqual("%PDF-"u8))
            throw new ArgumentException("Geçerli bir PDF dosyası gereklidir.");
        content.Position = 0;
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(content, cancellationToken));
        var slug = asset.StorageKey.Split('/')[1];
        // Immutable file keys avoid stale browser covers and retain the old PDF on failure.
        var key = $"catalogs/{slug}/{hash.ToLowerInvariant()}.pdf";
        var catalogTitle = string.IsNullOrWhiteSpace(title) ? asset.Translations.FirstOrDefault(x => x.LanguageCode == "tr")?.Title ?? Title(fileName) : title.Trim();
        if (catalogTitle.Length > 250) throw new ArgumentException("Katalog başlığı en fazla 250 karakter olabilir.");
        await using (var existing = await storage.OpenReadAsync(key, cancellationToken))
        {
            if (existing is null)
            {
                content.Position = 0;
                await storage.SaveAsync(key, content, length, cancellationToken);
            }
        }
        string? warning = null;
        var coverUrl = "/images/catalog-placeholder.webp";
        asset.ReplacePdfFile(fileName, key, length, hash);
        asset.SetCover(null);
        try
        {
            await EnsureCoverAsync(asset, catalogTitle, cancellationToken);
            coverUrl = await CoverUrlAsync(asset, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger?.LogWarning(exception, "Replacement catalog cover failed for {StorageKey}", key);
            warning = "PDF güncellendi ancak kapak üretilemedi.";
        }
        asset.SetTranslation("tr", catalogTitle);
        await db.SaveChangesAsync(cancellationToken);
        return new(asset.Id, catalogTitle, fileName, $"/media/{key}", coverUrl, warning);
    }

    private static void RenderCover(Stream output, Stream pdf)
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("PDF cover rendering is not supported on this platform.");
#pragma warning disable CA1416
        PDFtoImage.Conversion.SaveWebp(output, pdf, page: 0, options: new(Dpi: 120));
#pragma warning restore CA1416
    }
    public static string Slug(string fileName)
    {
        var known = Manifest.SingleOrDefault(item => Normalize(item.FileName) == Normalize(fileName));
        if (known is not null) return known.Slug;
        var title = Title(fileName).ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var chars = title.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        return System.Text.RegularExpressions.Regex.Replace(new string(chars), "-+", "-").Trim('-');
    }
    private static string Normalize(string fileName) => Path.GetFileName(fileName).Normalize(NormalizationForm.FormD).ToLowerInvariant();
}

public interface IPdfCoverRenderer
{
    void Render(Stream output, Stream pdf);
}
