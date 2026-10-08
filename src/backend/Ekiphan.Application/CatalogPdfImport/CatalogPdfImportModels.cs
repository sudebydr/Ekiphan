namespace Ekiphan.Application.CatalogPdfImport;

public sealed record CatalogPdfManifestItem(string Slug, string Title, string FileName);
public sealed record CatalogPdfPreviewFile(string EntryPath, string FileName, long Length, string? CatalogSlug, string SuggestedTitle, string Status, string? Error);
public sealed record CatalogPdfPreview(int PdfCount, int IgnoredCount, IReadOnlyList<CatalogPdfPreviewFile> Files);
public sealed record CatalogPdfDocument(Guid Id, string Slug, string Title, string FileName, string Url, string CoverUrl, DateTimeOffset CreatedAt);
public sealed record CatalogPdfExecutionResult(int Uploaded, int Created, int Skipped, int Failed);
public sealed record CatalogPdfCoverBackfillResult(int Created, int Skipped, int Failed);
public sealed record SingleCatalogPdfResult(Guid MediaAssetId, string Title, string FileName, string Url, string CoverUrl, string? Warning);

public interface ICatalogPdfImportService
{
    Task<CatalogPdfPreview> PreviewAsync(Stream content, string fileName, long length, CancellationToken cancellationToken = default);
    Task<CatalogPdfExecutionResult> ExecuteAsync(Stream content, string fileName, long length, IReadOnlyDictionary<string, string>? titles = null, CancellationToken cancellationToken = default);
    Task<SingleCatalogPdfResult> UploadSingleAsync(Stream content, string fileName, long length, string? title, CancellationToken cancellationToken = default);
    Task<SingleCatalogPdfResult> ReplaceAsync(Guid id, Stream content, string fileName, long length, string? title, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CatalogPdfDocument>> GetPublicDocumentsAsync(CancellationToken cancellationToken = default);
    Task<CatalogPdfCoverBackfillResult> BackfillCoversAsync(CancellationToken cancellationToken = default);
}
