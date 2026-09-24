using Ekiphan.Domain.Seo;

namespace Ekiphan.Application.Seo;

public sealed record SeoImportRow(
    SeoEntityType EntityType,
    Guid EntityId,
    string LanguageCode,
    string Slug,
    string? MetaTitle,
    string? MetaDescription,
    string? CanonicalUrl,
    string? OpenGraphTitle,
    string? OpenGraphDescription,
    Guid? OpenGraphMediaId,
    bool NoIndex,
    bool NoFollow);

public sealed record SeoImportPreviewItemDto(
    int RowNumber,
    SeoEntityType EntityType,
    Guid EntityId,
    string LanguageCode,
    string Slug,
    string? MetaTitle,
    string? MetaDescription,
    bool IsValid,
    string? WarningMessage,
    string? ErrorMessage);

public sealed record SeoImportPreviewDto(
    int TotalRows,
    IReadOnlyList<SeoImportPreviewItemDto> PreviewItems);

public sealed record SeoImportValidationResultDto(
    bool IsValid,
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    IReadOnlyList<string> GlobalErrors);

public sealed record SeoImportExecutionResultDto(
    Guid BatchId,
    int TotalRows,
    int SuccessRows,
    int ErrorRows,
    DateTimeOffset CompletedAt);

public sealed record SeoImportRollbackResultDto(
    Guid BatchId,
    bool Success,
    int RestoredCount,
    string? Message);

public sealed record SeoExportRequestDto(
    SeoEntityType? EntityType = null,
    string? LanguageCode = null,
    string Format = "csv",
    bool PublishedOnly = true);

public interface ISeoImportExportService
{
    Task<SeoImportPreviewDto> PreviewImportAsync(Stream stream, string fileType, CancellationToken cancellationToken);
    Task<SeoImportValidationResultDto> ValidateImportAsync(Stream stream, string fileType, CancellationToken cancellationToken);
    Task<SeoImportExecutionResultDto> ExecuteImportAsync(Stream stream, string fileType, Guid actorUserId, CancellationToken cancellationToken);
    Task<SeoImportRollbackResultDto> RollbackImportBatchAsync(Guid batchId, Guid actorUserId, CancellationToken cancellationToken);
    Task<byte[]> ExportAsync(SeoExportRequestDto request, CancellationToken cancellationToken);
}
