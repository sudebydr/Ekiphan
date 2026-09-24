namespace Ekiphan.Application.Catalog;

public static class ProductManagementErrorCodes
{
    public const string ProductNotFound = "PRODUCT_NOT_FOUND";
    public const string ProductArchived = "PRODUCT_ARCHIVED";
    public const string ProductWorkflowTransitionInvalid = "PRODUCT_WORKFLOW_TRANSITION_INVALID";
    public const string ProductPublishPermissionRequired = "PRODUCT_PUBLISH_PERMISSION_REQUIRED";
    public const string ProductDirectPublishNotAllowed = "PRODUCT_DIRECT_PUBLISH_NOT_ALLOWED";
    public const string ProductQualityBlocksPublishing = "PRODUCT_QUALITY_BLOCKS_PUBLISHING";
    public const string ProductSkuRequired = "PRODUCT_SKU_REQUIRED";
    public const string ProductSkuDuplicate = "PRODUCT_SKU_DUPLICATE";
    public const string ProductSlugDuplicate = "PRODUCT_SLUG_DUPLICATE";
    public const string ProductMainCategoryRequired = "PRODUCT_MAIN_CATEGORY_REQUIRED";
    public const string ProductBrandRequired = "PRODUCT_BRAND_REQUIRED";
    public const string ProductPrimaryImageRequired = "PRODUCT_PRIMARY_IMAGE_REQUIRED";
    public const string ProductRevisionNotFound = "PRODUCT_REVISION_NOT_FOUND";
    public const string ProductRevisionRestoreConflict = "PRODUCT_REVISION_RESTORE_CONFLICT";
    public const string ProductDuplicateSkuRequired = "PRODUCT_DUPLICATE_SKU_REQUIRED";
    public const string ProductBulkOperationInvalid = "PRODUCT_BULK_OPERATION_INVALID";
    public const string ProductBulkOperationNotFound = "PRODUCT_BULK_OPERATION_NOT_FOUND";
    public const string ProductBulkOperationAlreadyExecuted = "PRODUCT_BULK_OPERATION_ALREADY_EXECUTED";
    public const string ProductBulkItemLimitExceeded = "PRODUCT_BULK_ITEM_LIMIT_EXCEEDED";
    public const string ProductBulkPreviewExpired = "PRODUCT_BULK_PREVIEW_EXPIRED";
    public const string ProductBulkPreviewStale = "PRODUCT_BULK_PREVIEW_STALE";
    public const string ProductCategoryInvalid = "PRODUCT_CATEGORY_INVALID";
    public const string ProductCategoryReorderInvalid = "PRODUCT_CATEGORY_REORDER_INVALID";
    public const string ProductConcurrencyConflict = "PRODUCT_CONCURRENCY_CONFLICT";
    public const string ProductQualityRuleFailed = "PRODUCT_QUALITY_RULE_FAILED";
}
