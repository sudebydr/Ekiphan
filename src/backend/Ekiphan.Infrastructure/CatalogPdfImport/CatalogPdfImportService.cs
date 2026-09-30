using System.IO.Compression;
using System.Text;
using Ekiphan.Application.CatalogPdfImport;
using Ekiphan.Domain.Media;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Ekiphan.Application.Media;
using System.Security.Cryptography;

namespace Ekiphan.Infrastructure.CatalogPdfImport;

public sealed class CatalogPdfImportService(EkiphanDbContext db, IMediaFileStorage storage) : ICatalogPdfImportService
{
    // This manifest is intentionally the single source of truth for the existing /kataloglar cards.
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
        if (!Path.GetExtension(fileName).Equals(".zip", StringComparison.OrdinalIgnoreCase) || length is <= 0 or > 2L * 1024 * 1024 * 1024)
            throw new ArgumentException("A ZIP file up to 2 GB is required.");

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
                files.Add(new(entry.FullName, entry.Name, entry.Length, null, "Ignored", null));
                continue;
            }
            if (Path.IsPathRooted(entry.FullName) || entry.FullName.Contains("..", StringComparison.Ordinal) || entry.FullName.Contains('\\'))
                throw new ArgumentException("ZIP contains an unsafe entry path.");
            if (!Path.GetExtension(entry.Name).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                files.Add(new(entry.FullName, entry.Name, entry.Length, null, "Rejected", "Only PDF files are allowed."));
                continue;
            }
            var normalized = Normalize(entry.Name);
            if (!names.Add(normalized)) throw new ArgumentException("ZIP contains duplicate normalized PDF filenames.");
            if (entry.Length is <= 0 or > 300L * 1024 * 1024) throw new ArgumentException("A PDF exceeds the 300 MB limit.");
            await using var pdf = entry.Open();
            var signature = new byte[5];
            if (await pdf.ReadAsync(signature, cancellationToken) != signature.Length || !signature.AsSpan().SequenceEqual("%PDF-"u8))
                throw new ArgumentException($"'{entry.Name}' is not a valid PDF.");
            var match = Manifest.SingleOrDefault(item => Normalize(item.FileName) == normalized);
            files.Add(new(entry.FullName, entry.Name, entry.Length, match?.Slug, match is null ? "Unmatched" : "Matched", null));
        }
        if (files.Any(file => file.Status is "Rejected" or "Unmatched") || files.Count(file => file.Status == "Matched") != Manifest.Count)
            throw new ArgumentException("ZIP must contain exactly the approved catalog PDF manifest.");
        return new(Manifest.Count, files.Count(file => file.Status == "Ignored"), files);
    }

    public async Task<IReadOnlyList<CatalogPdfDocument>> GetPublicDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var keys = Manifest.ToDictionary(item => item.Slug, item => StorageKey(item.Slug), StringComparer.Ordinal);
        var available = await db.MediaAssets.AsNoTracking()
            .Where(asset => asset.AssetType == MediaAssetType.Pdf && asset.Status == MediaStatus.Active && asset.StorageKey != null)
            .Select(asset => asset.StorageKey!)
            .ToListAsync(cancellationToken);
        return keys.Where(item => available.Contains(item.Value, StringComparer.Ordinal))
            .Select(item => new CatalogPdfDocument(item.Key, $"/media/{item.Value}"))
            .ToArray();
    }

    public async Task<CatalogPdfExecutionResult> ExecuteAsync(Stream content, string fileName, long length, CancellationToken cancellationToken = default)
    {
        var preview = await PreviewAsync(content, fileName, length, cancellationToken);
        if (preview.Files.Any(file => file.Status is not "Matched" and not "Ignored") || preview.PdfCount != Manifest.Count)
            throw new ArgumentException("ZIP must contain exactly the approved catalog PDF manifest.");
        if (!content.CanSeek) throw new ArgumentException("Catalog ZIP stream must be seekable.");
        content.Position = 0;
        using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
        var keys = Manifest.Select(item => StorageKey(item.Slug)).ToArray();
        var existingKeys = (await db.MediaAssets.AsNoTracking()
            .Where(asset => asset.StorageKey != null && keys.Contains(asset.StorageKey))
            .Select(asset => asset.StorageKey!)
            .ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);
        var savedKeys = new List<string>();
        var created = 0;
        var skipped = 0;
        try
        {
            foreach (var item in Manifest)
            {
                var entry = archive.Entries.Single(entry => Normalize(entry.Name) == Normalize(item.FileName));
                var key = StorageKey(item.Slug);
                if (existingKeys.Contains(key))
                {
                    skipped++;
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
                asset.AddTranslation("tr", item.Title);
                db.MediaAssets.Add(asset);
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

    public static string StorageKey(string slug) => $"catalogs/{slug}/2026/{slug}.pdf";
    private static string Normalize(string fileName) => Path.GetFileName(fileName).Normalize(NormalizationForm.FormD).ToLowerInvariant();
}
