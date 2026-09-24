using Ekiphan.Domain.DataImport;

namespace Ekiphan.Application.DataImport;

public static class ProductImportField
{
    public const string Sku = "Sku";
    public const string ProductNameTr = "ProductNameTr";
    public const string MainCategory = "MainCategory";
    public const string Material = "Material";
    public const string LongDescriptionTr = "LongDescriptionTr";
    public const string ShortDescriptionTr = "ShortDescriptionTr";
    public const string Brand = "Brand";
    public const string Color = "Color";
    public const string Size = "Size";
    public const string Tags = "Tags";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(
        [Sku, ProductNameTr, MainCategory, Material, LongDescriptionTr,
         ShortDescriptionTr, Brand, Color, Size, Tags], StringComparer.Ordinal);
}

public sealed record ProductImportColumnMappingDto(string SourceHeader, string TargetField);
public sealed record ProductImportOptionsDto(bool SkipInvalidRows = false, bool RejectExistingSkus = true);
public sealed record ProductImportPreviewRequest(Stream Content, string FileName, string ContentType, long Length, Guid UserId);
public sealed record ProductImportPreviewDto(Guid BatchId, string UploadToken, string FileName,
    int TotalRowCount, IReadOnlyList<string> Headers, IReadOnlyList<string> NormalizedHeaders,
    IReadOnlyList<ProductImportColumnMappingDto> SuggestedMappings,
    IReadOnlyList<IReadOnlyDictionary<string, string?>> First20Rows);
public sealed record ProductImportValidateCommand(string UploadToken,
    IReadOnlyList<ProductImportColumnMappingDto> Mappings, ProductImportOptionsDto ImportOptions);
public sealed record ProductImportExecuteCommand(string ValidationToken);
public sealed record ProductImportRollbackCommand(Guid BatchId, Guid UserId);
public sealed record ProductImportRowErrorDto(int RowNumber, string? Sku, string Field, string Code, string Message);
public sealed record ProductImportValidationResultDto(Guid BatchId, int TotalRows, int ValidRows,
    int InvalidRows, IReadOnlyList<string> DuplicateSkus, IReadOnlyList<string> ExistingSkus,
    IReadOnlyList<string> MissingBrands, IReadOnlyList<string> MissingCategories,
    IReadOnlyList<string> MissingRequiredFields, IReadOnlyList<ProductImportRowErrorDto> RowErrors,
    string ValidationToken);
public sealed record ProductImportExecutionResultDto(Guid BatchId, ImportBatchStatus Status,
    int CreatedProducts, int FailedRows);
public sealed record ProductImportBatchDetailDto(Guid Id, string FileName, ImportBatchStatus Status,
    int TotalRows, int ValidRows, int ErrorRows, int CreatedProducts, int RollbackEligibleProducts,
    string? FailureReason, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt,
    DateTimeOffset? RolledBackAt, string? RollbackSummary);
public sealed record ProductImportRollbackResultDto(Guid BatchId, int RolledBackProducts,
    int SkippedModifiedProducts, ImportBatchStatus Status);

public sealed record ProductImportStoredUpload(Guid BatchId, string FileName, string ContentType,
    string Sha256, TabularImportDocument Document, DateTimeOffset ExpiresAt);
public sealed record ProductImportNormalizedRow(int RowNumber, string? Sku, string? ProductNameTr,
    string? MainCategory, string? Material, string? LongDescriptionTr,
    string? ShortDescriptionTr, string? Brand, string? Color, string? Size,
    IReadOnlyList<string> Tags, Guid? BrandId, Guid? CategoryId);
public sealed record ProductImportStoredValidation(Guid BatchId, string FileHash,
    IReadOnlyList<ProductImportNormalizedRow> ValidRows,
    IReadOnlyList<ProductImportRowErrorDto> Errors, ProductImportOptionsDto Options,
    DateTimeOffset ExpiresAt);

public interface IProductImportFileReader
{
    bool CanRead(string fileName);
    Task<TabularImportDocument> ReadAsync(Stream content, string fileName,
        ImportFileReadOptions options, CancellationToken cancellationToken = default);
}
public interface IProductImportMappingService
{
    string NormalizeHeader(string header);
    IReadOnlyList<ProductImportColumnMappingDto> Suggest(IReadOnlyList<string> headers);
}
public interface IProductImportUploadStore
{
    string Put(ProductImportStoredUpload upload);
    bool TryGet(string token, out ProductImportStoredUpload upload);
    string PutValidation(ProductImportStoredValidation validation);
    bool TryGetValidation(string token, out ProductImportStoredValidation validation);
    void Remove(string token);
}
public interface IProductImportRepository
{
    void AddBatch(ImportBatch batch);
    Task<ImportBatch?> GetBatchAsync(Guid id, bool tracking, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<string>> GetExistingSkusAsync(IEnumerable<string> skus, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, Guid>> GetBrandsAsync(IEnumerable<string> names, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, Guid>> GetCategoriesAsync(IEnumerable<string> names, CancellationToken cancellationToken = default);
    Task<Ekiphan.Domain.Catalog.Product?> GetProductAsync(Guid id, CancellationToken cancellationToken = default);
    void AddProduct(Ekiphan.Domain.Catalog.Product product);
    Task ExecuteTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
public interface IProductImportValidationService
{
    Task<ProductImportValidationResultDto> ValidateAsync(ProductImportValidateCommand command, CancellationToken cancellationToken = default);
}
public interface IProductImportExecutionService
{
    Task<ProductImportExecutionResultDto> ExecuteAsync(ProductImportExecuteCommand command, CancellationToken cancellationToken = default);
}
public interface IProductImportRollbackService
{
    Task<ProductImportRollbackResultDto> RollbackAsync(ProductImportRollbackCommand command, CancellationToken cancellationToken = default);
}
public interface IProductImportService
{
    Task<ProductImportPreviewDto> PreviewAsync(ProductImportPreviewRequest request, CancellationToken cancellationToken = default);
    Task<ProductImportBatchDetailDto?> GetBatchAsync(Guid batchId, CancellationToken cancellationToken = default);
    Task WriteErrorsCsvAsync(Guid batchId, Stream destination, CancellationToken cancellationToken = default);
}

public sealed class ProductImportTokenException(string message) : Exception(message);
public sealed class ProductImportConflictException(string message) : Exception(message);
