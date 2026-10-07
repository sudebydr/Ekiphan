using System.IO.Compression;
using System.Text;
using System.Globalization;
using Ekiphan.Application.CatalogPdfImport;
using Ekiphan.Domain.Media;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Ekiphan.Application.Media;
using System.Security.Cryptography;

namespace Ekiphan.Infrastructure.CatalogPdfImport;

public sealed class CatalogPdfImportService(EkiphanDbContext db, IMediaFileStorage storage, IPdfCoverRenderer? coverRenderer = null) : ICatalogPdfImportService
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
            .Select(asset => new { asset.Id, asset.StorageKey, asset.OriginalFileName, asset.CreatedAt,
                Title = asset.Translations.Where(t => t.LanguageCode == "tr").Select(t => t.Title).FirstOrDefault() })
            .ToListAsync(cancellationToken);
        var coverKeys = (await db.MediaAssets.AsNoTracking()
            .Where(asset => asset.AssetType == MediaAssetType.Image && asset.Status == MediaStatus.Active &&
                asset.StorageKey != null && asset.StorageKey.StartsWith("catalogs/") && asset.StorageKey.EndsWith("-cover.webp"))
            .Select(asset => asset.StorageKey!).ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);
        return assets.Select(asset => new CatalogPdfDocument(asset.Id,
            asset.StorageKey!.Split('/').Skip(1).FirstOrDefault() ?? asset.Id.ToString("N"),
            asset.Title ?? asset.OriginalFileName!, asset.OriginalFileName!,
            $"/media/{asset.StorageKey}",
            coverKeys.Contains(CoverStorageKey(asset.StorageKey.Split('/').Skip(1).First()))
                ? $"/media/{CoverStorageKey(asset.StorageKey.Split('/').Skip(1).First())}"
                : "/images/catalog-placeholder.webp", asset.CreatedAt)).ToArray();
    }

    public async Task<CatalogPdfCoverBackfillResult> BackfillCoversAsync(CancellationToken cancellationToken = default)
    {
        var documents = await db.MediaAssets
            .Where(asset => asset.AssetType == MediaAssetType.Pdf && asset.Status == MediaStatus.Active &&
                asset.StorageKey != null && asset.StorageKey.StartsWith("catalogs/"))
            .Select(asset => new
            {
                asset.StorageKey,
                Title = asset.Translations.Where(t => t.LanguageCode == "tr").Select(t => t.Title).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
        var coverKeys = (await db.MediaAssets.AsNoTracking()
            .Where(asset => asset.AssetType == MediaAssetType.Image && asset.Status == MediaStatus.Active &&
                asset.StorageKey != null && asset.StorageKey.StartsWith("catalogs/") && asset.StorageKey.EndsWith("-cover.webp"))
            .Select(asset => asset.StorageKey!).ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);
        var created = 0; var skipped = 0; var failed = 0;
        foreach (var document in documents)
        {
            var slug = document.StorageKey!.Split('/').Skip(1).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(slug)) { failed++; continue; }
            var coverKey = CoverStorageKey(slug);
            if (coverKeys.Contains(coverKey)) { skipped++; continue; }
            try
            {
                await using var pdf = await storage.OpenReadAsync(document.StorageKey!, cancellationToken);
                if (pdf is null) { failed++; continue; }
                await using var cover = new MemoryStream();
                if (coverRenderer is null) RenderCover(cover, pdf); else coverRenderer.Render(cover, pdf);
                cover.Position = 0;
                await storage.SaveAsync(coverKey, cover, cover.Length, cancellationToken);
                cover.Position = 0;
                var hash = Convert.ToHexString(await SHA256.HashDataAsync(cover, cancellationToken));
                var title = document.Title ?? slug;
                var asset = MediaAsset.CreateFile(Guid.NewGuid(), MediaAssetType.Image, $"{slug}-cover.webp", coverKey,
                    "image/webp", cover.Length, hash, storage.ProviderName);
                asset.AddTranslation("tr", $"{title} kapak", $"{title} katalog kapağı");
                db.MediaAssets.Add(asset); coverKeys.Add(coverKey); created++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failed++;
            }
        }
        await db.SaveChangesAsync(cancellationToken);
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
        var existingKeys = (await db.MediaAssets.AsNoTracking()
            .Where(asset => asset.StorageKey != null && keys.Contains(asset.StorageKey))
            .Select(asset => asset.StorageKey!)
            .ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);
        var savedKeys = new List<string>();
        var created = 0;
        var skipped = 0;
        try
        {
            foreach (var item in items)
            {
                var entry = archive.Entries.Single(entry => entry.FullName == item.EntryPath);
                var key = StorageKey(item.CatalogSlug!);
                if (existingKeys.Contains(key))
                {
                    await using var storedFile = await storage.OpenReadAsync(key, cancellationToken);
                    if (storedFile is not null)
                    {
                        skipped++;
                        continue;
                    }

                    await using var replacement = entry.Open();
                    await storage.SaveAsync(key, replacement, entry.Length, cancellationToken);
                    savedKeys.Add(key);
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
                    await using var pdfForCover = entry.Open();
                    await using var cover = new MemoryStream();
                    if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
                        throw new PlatformNotSupportedException("PDF cover rendering is not supported on this platform.");
#pragma warning disable CA1416
                    PDFtoImage.Conversion.SaveWebp(cover, pdfForCover, page: 0, options: new(Dpi: 120));
#pragma warning restore CA1416
                    cover.Position = 0;
                    var coverKey = CoverStorageKey(item.CatalogSlug!);
                    await storage.SaveAsync(coverKey, cover, cover.Length, cancellationToken);
                    savedKeys.Add(coverKey);
                    cover.Position = 0;
                    var coverHash = Convert.ToHexString(await SHA256.HashDataAsync(cover, cancellationToken));
                    var coverAsset = MediaAsset.CreateFile(Guid.NewGuid(), MediaAssetType.Image,
                        $"{item.CatalogSlug}-cover.webp", coverKey, "image/webp", cover.Length, coverHash, storage.ProviderName);
                    coverAsset.AddTranslation("tr", $"{(string.IsNullOrWhiteSpace(chosenTitle) ? item.SuggestedTitle : chosenTitle)} kapak",
                        $"{(string.IsNullOrWhiteSpace(chosenTitle) ? item.SuggestedTitle : chosenTitle)} katalog kapağı");
                    db.MediaAssets.Add(coverAsset);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
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
                content.Position = 0;
                await using var cover = new MemoryStream();
                if (coverRenderer is null) RenderCover(cover, content);
                else coverRenderer.Render(cover, content);
                cover.Position = 0;
                await storage.SaveAsync(coverKey, cover, cover.Length, cancellationToken);
                savedKeys.Add(coverKey);
                cover.Position = 0;
                var coverHash = Convert.ToHexString(await SHA256.HashDataAsync(cover, cancellationToken));
                var coverAsset = MediaAsset.CreateFile(Guid.NewGuid(), MediaAssetType.Image,
                    $"{slug}-cover.webp", coverKey, "image/webp", cover.Length, coverHash, storage.ProviderName);
                coverAsset.AddTranslation("tr", $"{catalogTitle} kapak", $"{catalogTitle} katalog kapağı");
                db.MediaAssets.Add(coverAsset);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                warning = "PDF yüklendi ancak kapak üretilemedi; placeholder kullanılacak.";
            }

            await db.SaveChangesAsync(cancellationToken);
            return new(asset.Id, catalogTitle, fileName, $"/media/{pdfKey}",
                savedKeys.Contains(coverKey) ? $"/media/{coverKey}" : "/images/catalog-placeholder.webp", warning);
        }
        catch
        {
            foreach (var key in savedKeys) await storage.DeleteAsync(key, CancellationToken.None);
            throw;
        }
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
