using Ekiphan.Domain.Catalog;

namespace Ekiphan.Application.Catalog;

public sealed record ProductWorkflowTransitionResult(
    bool Success,
    ProductWorkflowStatus CurrentStatus,
    string? ErrorMessage = null);

public sealed record ProductRevisionListItemDto(
    Guid RevisionId,
    int VersionNumber,
    ProductRevisionChangeType ChangeType,
    string? ChangedFieldsJson,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string? Reason,
    ProductRevisionSource Source);

public sealed record ProductRevisionDetailDto(
    Guid RevisionId,
    Guid ProductId,
    int VersionNumber,
    ProductRevisionChangeType ChangeType,
    string SnapshotJson,
    string? ChangedFieldsJson,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string? Reason,
    string? CorrelationId,
    ProductRevisionSource Source);

public sealed record ProductRevisionSnapshotDto(
    int SchemaVersion,
    Guid ProductId,
    string Sku,
    Guid? BrandId,
    Guid? PrimaryCategoryId,
    ProductWorkflowStatus WorkflowStatus,
    bool IsPublished,
    IReadOnlyList<ProductCategorySnapshotDto> Categories,
    IReadOnlyList<ProductTranslationSnapshotDto> Translations,
    IReadOnlyList<ProductTagSnapshotDto> Tags,
    IReadOnlyList<ProductVariantGroupSnapshotDto> VariantGroups,
    IReadOnlyList<ProductVariantSnapshotDto> Variants);

public sealed record ProductCategorySnapshotDto(Guid CategoryId, bool IsPrimary, int SortOrder);
public sealed record ProductTranslationSnapshotDto(string LanguageCode, string Name, string Slug, string? ShortDescription, string? LongDescription, string? MetaTitle, string? MetaDescription, string? CanonicalUrl, bool NoIndex, bool NoFollow, string? OpenGraphTitle, string? OpenGraphDescription, Guid? OpenGraphImageMediaId);
public sealed record ProductTagSnapshotDto(Guid TagId, int SortOrder);
public sealed record ProductVariantGroupSnapshotDto(Guid GroupId, string Code, int SortOrder);
public sealed record ProductVariantSnapshotDto(Guid VariantId, string Sku, int SortOrder, bool IsActive, Guid? MediaAssetId);

public sealed record DuplicateProductCommand(
    Guid ProductId,
    string NewSku,
    bool CopyTranslations = true,
    bool CopyCategories = true,
    bool CopyAttributes = true,
    bool CopyTags = true,
    bool CopyVariants = false,
    bool CopyMediaRelations = true,
    bool CopySeoFields = false,
    bool CopyRelatedProducts = false,
    Guid ActorUserId = default);

public sealed record DuplicateProductResultDto(
    Guid NewProductId,
    string NewSku,
    ProductWorkflowStatus WorkflowStatus);

public sealed record ProductBulkPreviewCommand(
    ProductBulkOperationType OperationType,
    IReadOnlyList<Guid> ProductIds,
    IDictionary<string, object?>? Parameters,
    string? Reason);

public sealed record ProductBulkPreviewResultDto(
    int TotalProducts,
    int EligibleProducts,
    int SkippedProducts,
    int InvalidProducts,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> ValidationErrors,
    IReadOnlyList<Guid> AffectedProductIds,
    string PreviewToken,
    DateTimeOffset ExpiresAt);

public sealed record ProductBulkExecuteCommand(
    ProductBulkOperationType OperationType,
    IReadOnlyList<Guid> ProductIds,
    IDictionary<string, object?>? Parameters,
    string? Reason,
    bool ExecuteAsBackgroundJob = false,
    string? PreviewToken = null,
    Guid ActorUserId = default);

public sealed record ProductBulkExecutionResultDto(
    Guid BulkOperationId,
    ProductBulkOperationStatus Status,
    bool IsBackgroundJob,
    int TotalCount,
    int SuccessCount,
    int FailedCount,
    int SkippedCount,
    string? ErrorMessage);

public sealed record ProductBulkOperationDetailDto(
    Guid OperationId,
    ProductBulkOperationType OperationType,
    ProductBulkOperationStatus Status,
    int TotalCount,
    int SuccessCount,
    int FailedCount,
    int SkippedCount,
    Guid RequestedByUserId,
    DateTimeOffset RequestedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    double ProgressPercentage,
    string? ErrorMessage,
    IReadOnlyList<ProductBulkOperationItemDto> Items);

public sealed record ProductBulkOperationItemDto(
    Guid Id,
    Guid ProductId,
    ProductBulkOperationItemStatus Status,
    string? ErrorCode,
    string? ErrorMessage,
    int? PreviousVersionNumber,
    int? NewVersionNumber,
    DateTimeOffset CompletedAt);

public sealed record ReorderCategoryProductsCommand(
    Guid CategoryId,
    IReadOnlyList<ReorderCategoryProductItemDto> Items,
    byte[] RowVersion,
    Guid ActorUserId);

public sealed record ReorderCategoryProductItemDto(
    Guid ProductId,
    int SortOrder);

public sealed record ProductQualityResultDto(
    Guid ProductId,
    int QualityScore,
    bool BlocksPublishing,
    DateTimeOffset EvaluatedAt,
    IReadOnlyList<ProductQualityIssueDto> Issues);

public sealed record ProductQualityIssueDto(
    string Code,
    ProductQualitySeverity Severity,
    string? Field,
    string Message,
    string? SuggestedAction,
    bool BlocksPublishing);

public enum ProductQualitySeverity
{
    Information = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}

public sealed record ProductQualitySummaryDto(
    int TotalEvaluated,
    int AverageQualityScore,
    int ProductsBlockingPublishing,
    IReadOnlyList<ProductQualityResultDto> Results);

public sealed record ProductQualityDashboardDto(
    int TotalProducts,
    int ProductsWithCriticalIssues,
    int ProductsBlockingPublishing,
    int ProductsMissingImages,
    int ProductsMissingEnglishContent,
    int ProductsMissingSeo,
    int DuplicateSkuCount,
    int DuplicateSlugCount,
    double AverageQualityScore,
    IReadOnlyList<ProductQualityDashboardItemDto> Items);

public sealed record ProductQualityDashboardItemDto(
    Guid ProductId,
    string Sku,
    string Name,
    ProductWorkflowStatus WorkflowStatus,
    int QualityScore,
    bool BlocksPublishing,
    int IssueCount);

public sealed record ProductQualityFilter(
    string? IssueCode = null,
    ProductQualitySeverity? Severity = null,
    ProductWorkflowStatus? WorkflowStatus = null,
    Guid? BrandId = null,
    Guid? CategoryId = null,
    int? MinScore = null,
    int? MaxScore = null,
    bool? MissingImage = null,
    bool? MissingEnglish = null,
    bool? MissingSeo = null,
    bool? DuplicateSku = null,
    bool? DuplicateSlug = null,
    bool? BlocksPublishing = null,
    int Page = 1,
    int PageSize = 20);

public interface IProductWorkflowService
{
    bool CanTransition(
        ProductWorkflowStatus currentStatus,
        ProductWorkflowStatus targetStatus,
        IReadOnlyCollection<string> permissions);

    IReadOnlyCollection<ProductWorkflowStatus> GetAllowedTransitions(
        ProductWorkflowStatus currentStatus,
        IReadOnlyCollection<string> permissions);

    Task<ProductWorkflowTransitionResult> TransitionAsync(
        Guid productId,
        ProductWorkflowStatus targetStatus,
        string? reason,
        byte[] rowVersion,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public interface IProductRevisionService
{
    Task<ProductRevision> CreateRevisionAsync(
        Product product,
        ProductRevisionChangeType changeType,
        Guid actorUserId,
        string? reason = null,
        string? changedFieldsJson = null,
        ProductRevisionSource source = ProductRevisionSource.Manual,
        Guid? importBatchId = null,
        Guid? bulkOperationId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductRevisionListItemDto>> GetRevisionsAsync(
        Guid productId,
        ProductRevisionChangeType? changeType = null,
        Guid? userId = null,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ProductRevisionDetailDto?> GetRevisionAsync(
        Guid productId,
        Guid revisionId,
        CancellationToken cancellationToken = default);
}

public interface IProductSnapshotSerializer
{
    string Serialize(Product product);
    ProductRevisionSnapshotDto Deserialize(string json);
}

public interface IProductRevisionRestoreService
{
    Task<ProductWorkflowTransitionResult> RestoreRevisionAsync(
        Guid productId,
        Guid revisionId,
        string reason,
        byte[] rowVersion,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public interface IProductDuplicationService
{
    Task<DuplicateProductResultDto> DuplicateAsync(
        DuplicateProductCommand command,
        CancellationToken cancellationToken = default);
}

public interface IProductBulkOperationService
{
    Task<ProductBulkPreviewResultDto> PreviewAsync(
        ProductBulkPreviewCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductBulkExecutionResultDto> ExecuteAsync(
        ProductBulkExecuteCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductBulkOperationDetailDto?> GetOperationAsync(
        Guid operationId,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportErrorsCsvAsync(
        Guid operationId,
        CancellationToken cancellationToken = default);
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Queue suffix is domain expressive.")]
public interface IProductBulkOperationQueue
{
    Task EnqueueAsync(
        Guid bulkOperationId,
        CancellationToken cancellationToken = default);
}

public interface IProductQualityService
{
    Task<ProductQualityResultDto> EvaluateAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<ProductQualitySummaryDto> EvaluateBatchAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default);

    Task<ProductQualityDashboardDto> GetDashboardAsync(
        ProductQualityFilter filter,
        CancellationToken cancellationToken = default);
}

public sealed record ProductQualityContext(
    Product Product,
    IReadOnlyList<Product> AllActiveProducts);

public interface IProductQualityRule
{
    string Code { get; }

    Task<IReadOnlyCollection<ProductQualityIssueDto>> EvaluateAsync(
        ProductQualityContext context,
        CancellationToken cancellationToken = default);
}

public interface IProductSkuNormalizationService
{
    string Normalize(string sku);
}

public interface IProductSlugValidationService
{
    Task<bool> IsSlugUniqueAsync(
        string slug,
        string languageCode,
        Guid? excludeProductId = null,
        CancellationToken cancellationToken = default);
}

public interface ICategoryProductOrderingService
{
    Task ReorderAsync(
        ReorderCategoryProductsCommand command,
        CancellationToken cancellationToken = default);
}

public interface IProductCacheInvalidationService
{
    Task InvalidateProductCacheAsync(Guid productId, CancellationToken cancellationToken = default);
    Task InvalidateCategoryCacheAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task InvalidateBrandCacheAsync(Guid? brandId, CancellationToken cancellationToken = default);
}

public interface IProductSearchIndexService
{
    Task QueueIndexUpdateAsync(Guid productId, CancellationToken cancellationToken = default);
}
