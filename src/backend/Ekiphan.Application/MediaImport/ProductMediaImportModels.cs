using Ekiphan.Domain.Media;

namespace Ekiphan.Application.MediaImport;

public sealed class ProductMediaImportOptions
{
    public const string SectionName = "ProductMediaImport";
    public int MaxZipSizeMb { get; set; } = 250;
    public int MaxFileCount { get; set; } = 5000;
    public int MaxExtractedSizeMb { get; set; } = 2000;
    public int MaxSingleImageSizeMb { get; set; } = 4;
    public int MaxCompressionRatio { get; set; } = 100;
    public int UploadTokenLifetimeMinutes { get; set; } = 60;
    public int ValidationTokenLifetimeMinutes { get; set; } = 30;
    public int TemporaryFileLifetimeMinutes { get; set; } = 120;
    public long MaxZipBytes => MaxZipSizeMb * 1024L * 1024L;
    public long MaxExtractedBytes => MaxExtractedSizeMb * 1024L * 1024L;
    public long MaxSingleImageBytes => MaxSingleImageSizeMb * 1024L * 1024L;
}

public enum ProductMediaDetectedPosition { Main, Detail, Front, Back, Side, Gallery, Unknown }
public enum ProductMediaPreviewFileStatus { Ready, Unsupported, Invalid, Duplicate, MissingSku, SecurityRejected, Skipped }
public enum ProductMediaValidationFileStatus { Valid, Unmatched, Duplicate, Invalid, Conflict, Ignored }
public enum ProductMediaMatchSource { FileName, Manual, ExistingMapping, None }

public sealed record ProductMediaImportPreviewRequest(Stream Content, string FileName, string ContentType, long Length, Guid UserId);
public sealed record ProductMediaImportPreviewDto(string UploadToken, string FileName, long ZipFileSize,
    int TotalEntryCount, int SupportedImageCount, int IgnoredFileCount, int InvalidFileCount,
    int DuplicateFileCount, int SkippedEntryCount, int ExtractedSkuCount, IReadOnlyList<ProductMediaImportFilePreviewDto> Files,
    string Summary);
public sealed record ProductMediaImportFilePreviewDto(string TemporaryFileId, string OriginalFileName,
    string NormalizedFileName, string Extension, long FileSize, string? ExtractedSku,
    ProductMediaDetectedPosition DetectedPosition, int SuggestedSortOrder, bool SuggestedIsPrimary,
    ProductMediaPreviewFileStatus Status, string? ErrorCode, string? ErrorMessage);
public sealed record ProductMediaManualMappingDto(string TemporaryFileId, Guid? ProductId, string? Sku, int SortOrder, bool IsPrimary);
public sealed record ProductMediaImportOptionsDto(bool SkipUnmatchedFiles = true,
    bool ReplaceExistingPrimaryImage = false, bool SkipDuplicateContent = true);
public sealed record ProductMediaImportValidateCommand(string UploadToken,
    IReadOnlyList<ProductMediaManualMappingDto>? ManualMappings,
    ProductMediaImportOptionsDto? ImportOptions);
public sealed record ProductMediaImportValidationResultDto(string ValidationToken, int TotalFiles,
    int MatchedFiles, int UnmatchedFiles, int DuplicateFiles, int InvalidFiles, int IgnoredFiles,
    int ProductsWithImages, int ProductsWithoutPrimaryImage, int Conflicts,
    IReadOnlyList<ProductMediaImportFileValidationDto> Files, string Summary);
public sealed record ProductMediaImportFileValidationDto(string TemporaryFileId, string OriginalFileName,
    string? ExtractedSku, Guid? MatchedProductId, string? MatchedProductSku, string? MatchedProductName,
    ProductMediaMatchSource MatchSource, int SortOrder, bool IsPrimary, string ContentHash,
    ProductMediaValidationFileStatus Status, IReadOnlyList<ProductMediaImportErrorDto> Errors,
    IReadOnlyList<ProductMediaImportWarningDto> Warnings, Guid? ExistingMediaAssetId = null);
public sealed record ProductMediaImportExecuteCommand(string ValidationToken);
public sealed record ProductMediaImportExecutionResultDto(Guid BatchId, ProductMediaImportBatchStatus Status,
    int ImportedFiles, int SkippedFiles, int ErrorFiles, IReadOnlyList<ProductMediaImportErrorDto> Errors);
public sealed record ProductMediaImportBatchDetailDto(Guid BatchId, string FileName,
    ProductMediaImportBatchStatus Status, Guid CreatedBy, DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt, int TotalFiles, int MatchedFiles,
    int ImportedFiles, int SkippedFiles, int ErrorFiles, int RollbackEligibleFiles,
    IReadOnlyList<ProductMediaImportBatchItemDto> Items, IReadOnlyDictionary<string, int> ErrorSummary);
public sealed record ProductMediaImportBatchItemDto(string FileName, string? Sku, Guid? ProductId,
    string? ProductName, Guid? MediaAssetId, int SortOrder, bool IsPrimary,
    ProductMediaImportBatchItemStatus Status, string? ErrorCode, string? ErrorMessage);
public sealed record ProductMediaImportRollbackCommand(Guid BatchId, Guid UserId);
public sealed record ProductMediaImportRollbackResultDto(Guid BatchId, int RolledBackItemCount,
    int SkippedItemCount, int ArchivedMediaCount, int DeletedPhysicalFileCount,
    IReadOnlyList<ProductMediaImportErrorDto> Errors, IReadOnlyList<ProductMediaImportBatchItemDto> SkippedItems);
public sealed record ProductMediaImportErrorDto(string Code, string? Field, string Message);
public sealed record ProductMediaImportWarningDto(string Code, string? Field, string Message);

public sealed record ProductMediaSkuParseResult(string? Sku, ProductMediaDetectedPosition Position,
    int SuggestedSortOrder, bool SuggestedIsPrimary);
public sealed record ProductMediaArchiveEntry(string TemporaryFileId, string OriginalFileName,
    string NormalizedFileName, string Extension, string ContentType, long Length, long CompressedLength,
    string Sha256, ProductMediaSkuParseResult Parsed,
    ProductMediaPreviewFileStatus Status, string? ErrorCode, string? ErrorMessage);
public sealed record ProductMediaArchiveReadResult(string ContainerId, string FileName, long Length, string Sha256,
    int TotalEntries, IReadOnlyList<ProductMediaArchiveEntry> Entries);
public sealed record ProductMediaStoredUpload(Guid BatchId, ProductMediaArchiveReadResult Archive,
    Guid UserId, DateTimeOffset ExpiresAt);
public sealed record ProductMediaStoredValidation(Guid BatchId, string UploadToken,
    ProductMediaImportOptionsDto Options, IReadOnlyList<ProductMediaImportFileValidationDto> Files,
    DateTimeOffset ExpiresAt, bool Used = false);
public sealed record ProductMediaProductMatch(Guid Id, string Sku, string Name, bool IsDeleted,
    bool IsPublished, DateTimeOffset UpdatedAt, bool HasPrimaryImage);
public sealed record ProductMediaSkuResolution(ProductMediaProductMatch? Product, bool IsAmbiguous);
public sealed record ProductMediaContentLink(Guid ProductId, string ContentHash);

public interface IProductMediaImportArchiveReader
{
    Task<ProductMediaArchiveReadResult> ReadAsync(string containerId, Stream content, string fileName, string contentType,
        long length, CancellationToken cancellationToken = default);
}
public interface IProductMediaSkuParser { ProductMediaSkuParseResult Parse(string fileName); }
public interface IProductMediaSkuResolver
{
    IReadOnlyList<string> GetCandidateSkus(string fileName);
    ProductMediaSkuResolution Resolve(string fileName, IReadOnlyCollection<ProductMediaProductMatch> products);
}
public interface IProductMediaImportValidationService
{ Task<ProductMediaImportValidationResultDto> ValidateAsync(ProductMediaImportValidateCommand command, CancellationToken cancellationToken = default); }
public interface IProductMediaImportExecutionService
{ Task<ProductMediaImportExecutionResultDto> ExecuteAsync(ProductMediaImportExecuteCommand command, CancellationToken cancellationToken = default); }
public interface IProductMediaImportRollbackService
{ Task<ProductMediaImportRollbackResultDto> RollbackAsync(ProductMediaImportRollbackCommand command, CancellationToken cancellationToken = default); }
public interface IProductMediaImportService
{
    Task<ProductMediaImportPreviewDto> PreviewAsync(ProductMediaImportPreviewRequest request, CancellationToken cancellationToken = default);
    Task<ProductMediaImportBatchDetailDto?> GetBatchAsync(Guid batchId, CancellationToken cancellationToken = default);
    Task WriteErrorsCsvAsync(Guid batchId, Stream output, CancellationToken cancellationToken = default);
}
public interface IProductMediaImportRepository
{
    Task AddBatchAsync(ProductMediaImportBatch batch, CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductMediaProductMatch>> FindProductsAsync(IReadOnlyCollection<string> skus,
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, Guid>> FindMediaAssetsByHashesAsync(IReadOnlyCollection<string> hashes, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<ProductMediaContentLink>> FindProductMediaContentLinksAsync(IReadOnlyCollection<Guid> productIds,
        IReadOnlyCollection<string> hashes, CancellationToken cancellationToken = default);
    Task<ProductMediaImportBatch?> GetBatchAsync(Guid id, bool tracked, CancellationToken cancellationToken = default);
    Task<string?> GetProductNameAsync(Guid id, CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
    Task AddProductMediaAsync(Guid productId, Guid mediaAssetId, Guid batchId, int sortOrder,
        bool isPrimary, bool replaceExistingPrimary, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<(bool Removed, bool ArchiveAsset, string? StorageKey)> RollbackItemAsync(ProductMediaImportBatchItem item,
        DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<string?> ArchiveUnassignedMediaAsync(Guid mediaAssetId, DateTimeOffset now,
        CancellationToken cancellationToken = default);
}
public interface ITemporaryProductMediaStorage
{
    Task StoreFileAsync(string containerId, string temporaryFileId, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string containerId, string temporaryFileId, CancellationToken cancellationToken = default);
    Task DeleteAsync(string containerId, CancellationToken cancellationToken = default);
}
public interface IProductMediaImportTokenService
{
    string CreateUpload(ProductMediaStoredUpload upload);
    bool TryGetUpload(string token, out ProductMediaStoredUpload upload);
    string CreateValidation(ProductMediaStoredValidation validation);
    bool TryGetValidation(string token, out ProductMediaStoredValidation validation);
    bool TryUseValidation(string token, out ProductMediaStoredValidation validation);
}
public interface IProductMediaDuplicateDetector
{ string ComputeHash(ReadOnlySpan<byte> content); }

public sealed class ProductMediaImportTokenException(string message) : Exception(message);
public sealed class ProductMediaImportSecurityException(string message) : Exception(message);
public sealed class ProductMediaImportConflictException(string message) : Exception(message);
