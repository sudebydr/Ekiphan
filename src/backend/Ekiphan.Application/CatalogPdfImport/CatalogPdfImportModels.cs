namespace Ekiphan.Application.CatalogPdfImport;

public sealed record CatalogPdfManifestItem(string Slug, string Title, string FileName);
public sealed record CatalogPdfPreviewFile(string EntryPath, string FileName, long Length, string? CatalogSlug, string Status, string? Error);
public sealed record CatalogPdfPreview(int PdfCount, int IgnoredCount, IReadOnlyList<CatalogPdfPreviewFile> Files);
public sealed record CatalogPdfDocument(string Slug, string Url);
public sealed record CatalogPdfExecutionResult(int Uploaded, int Created, int Skipped, int Failed);

public interface ICatalogPdfImportService
{
    Task<CatalogPdfPreview> PreviewAsync(Stream content, string fileName, long length, CancellationToken cancellationToken = default);
    Task<CatalogPdfExecutionResult> ExecuteAsync(Stream content, string fileName, long length, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CatalogPdfDocument>> GetPublicDocumentsAsync(CancellationToken cancellationToken = default);
}
