namespace Ekiphan.Application.Content;

public static class CmsManagementErrorCodes
{
    public const string CmsEntityNotFound = "CMS_ENTITY_NOT_FOUND";
    public const string CmsWorkflowTransitionInvalid = "CMS_WORKFLOW_TRANSITION_INVALID";
    public const string CmsPublishValidationFailed = "CMS_PUBLISH_VALIDATION_FAILED";
    public const string CmsScheduleDateInvalid = "CMS_SCHEDULE_DATE_INVALID";
    public const string CmsSlugDuplicate = "CMS_SLUG_DUPLICATE";
    public const string CmsRevisionNotFound = "CMS_REVISION_NOT_FOUND";
    public const string CmsRevisionRestoreConflict = "CMS_REVISION_RESTORE_CONFLICT";
    public const string CmsPreviewTokenInvalid = "CMS_PREVIEW_TOKEN_INVALID";
    public const string CmsPreviewTokenExpired = "CMS_PREVIEW_TOKEN_EXPIRED";
    public const string CmsPreviewTokenAlreadyUsed = "CMS_PREVIEW_TOKEN_ALREADY_USED";

    public const string ReferenceNotFound = "REFERENCE_NOT_FOUND";
    public const string ReferenceCoverRequired = "REFERENCE_COVER_REQUIRED";
    public const string ReferenceProductAlreadyExists = "REFERENCE_PRODUCT_ALREADY_EXISTS";
    public const string ReferenceMediaAlreadyExists = "REFERENCE_MEDIA_ALREADY_EXISTS";

    public const string ShowroomNotFound = "SHOWROOM_NOT_FOUND";
    public const string ShowroomEmbedProviderNotAllowed = "SHOWROOM_EMBED_PROVIDER_NOT_ALLOWED";
    public const string ShowroomEmbedInvalid = "SHOWROOM_EMBED_INVALID";
    public const string ShowroomHotspotInvalid = "SHOWROOM_HOTSPOT_INVALID";
    public const string ShowroomProductNotFound = "SHOWROOM_PRODUCT_NOT_FOUND";

    public const string BannerNotFound = "BANNER_NOT_FOUND";
    public const string BannerGroupNotFound = "BANNER_GROUP_NOT_FOUND";
    public const string BannerDateRangeInvalid = "BANNER_DATE_RANGE_INVALID";
    public const string BannerMediaRequired = "BANNER_MEDIA_REQUIRED";
    public const string BannerUrlInvalid = "BANNER_URL_INVALID";

    public const string SettingKeyNotFound = "SETTING_KEY_NOT_FOUND";
    public const string SettingValueInvalid = "SETTING_VALUE_INVALID";
    public const string SettingSensitiveValueNotAllowed = "SETTING_SENSITIVE_VALUE_NOT_ALLOWED";
    public const string SettingConcurrencyConflict = "SETTING_CONCURRENCY_CONFLICT";

    public const string CustomerLogoNotFound = "CUSTOMER_LOGO_NOT_FOUND";
    public const string CustomerLogoDuplicate = "CUSTOMER_LOGO_DUPLICATE";
    public const string CustomerLogoMediaRequired = "CUSTOMER_LOGO_MEDIA_REQUIRED";

    public const string FooterColumnNotFound = "FOOTER_COLUMN_NOT_FOUND";
    public const string FooterLinkNotFound = "FOOTER_LINK_NOT_FOUND";
    public const string FooterUrlInvalid = "FOOTER_URL_INVALID";

    public const string CmsReorderInvalid = "CMS_REORDER_INVALID";
    public const string CmsConcurrencyConflict = "CMS_CONCURRENCY_CONFLICT";
}
