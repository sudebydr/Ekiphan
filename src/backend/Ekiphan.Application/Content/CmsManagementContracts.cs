using Ekiphan.Domain.Content;

namespace Ekiphan.Application.Content;

#region DTOs

public sealed record ContentWorkflowResultDto(
    bool Success,
    ContentWorkflowStatus WorkflowStatus,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? PublishAt,
    string? ErrorMessage);

public sealed record ReferenceProjectTranslationDto(
    string LanguageCode,
    string Title,
    string Slug,
    string? ShortDescription,
    string? LongDescription,
    string? MetaTitle,
    string? MetaDescription,
    string? CanonicalUrl,
    string? OpenGraphTitle,
    string? OpenGraphDescription,
    Guid? OpenGraphMediaAssetId);

public sealed record ReferenceProjectMediaDto(
    Guid Id,
    Guid MediaAssetId,
    int SortOrder,
    bool IsCover,
    string? CaptionTr,
    string? CaptionEn,
    DateTimeOffset CreatedAt);

public sealed record ReferenceProjectProductDto(
    Guid ProductId,
    string Sku,
    string? ProductName,
    int SortOrder,
    string? Description);

public sealed record ReferenceProjectListItemDto(
    Guid Id,
    string CustomerName,
    DateTimeOffset? ProjectDate,
    string? Location,
    Guid? CoverMediaAssetId,
    ContentWorkflowStatus WorkflowStatus,
    DateTimeOffset? PublishAt,
    DateTimeOffset? PublishedAt,
    int SortOrder,
    bool IsFeatured,
    DateTimeOffset CreatedAt,
    byte[] RowVersion);

public sealed record ReferenceProjectDetailDto(
    Guid Id,
    string CustomerName,
    DateTimeOffset? ProjectDate,
    string? Location,
    Guid? CoverMediaAssetId,
    ContentWorkflowStatus WorkflowStatus,
    DateTimeOffset? PublishAt,
    DateTimeOffset? PublishedAt,
    Guid? PublishedByUserId,
    int SortOrder,
    bool IsFeatured,
    DateTimeOffset CreatedAt,
    Guid CreatedByUserId,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedByUserId,
    byte[] RowVersion,
    IReadOnlyList<ReferenceProjectTranslationDto> Translations,
    IReadOnlyList<ReferenceProjectMediaDto> Media,
    IReadOnlyList<ReferenceProjectProductDto> Products);

public sealed record CustomerLogoDto(
    Guid Id,
    string Name,
    string? WebsiteUrl,
    Guid MediaAssetId,
    string? AltTextTr,
    string? AltTextEn,
    int SortOrder,
    bool IsActive,
    bool IsFeatured,
    DateTimeOffset CreatedAt,
    byte[] RowVersion);

public sealed record ShowroomTranslationDto(
    string LanguageCode,
    string Title,
    string Slug,
    string? ShortDescription,
    string? LongDescription,
    string? MetaTitle,
    string? MetaDescription,
    string? OpenGraphTitle,
    string? OpenGraphDescription,
    Guid? OpenGraphMediaAssetId);

public sealed record ShowroomMediaDto(
    Guid Id,
    Guid MediaAssetId,
    int SortOrder,
    string? CaptionTr,
    string? CaptionEn,
    DateTimeOffset CreatedAt);

public sealed record ShowroomHotspotDto(
    Guid Id,
    Guid ShowroomId,
    Guid? ProductId,
    string? ProductSku,
    string? ProductName,
    string? TitleTr,
    string? TitleEn,
    string? DescriptionTr,
    string? DescriptionEn,
    double PositionX,
    double PositionY,
    double PositionZ,
    string SceneIdentifier,
    int SortOrder,
    bool IsActive,
    byte[] RowVersion);

public sealed record ShowroomListItemDto(
    Guid Id,
    Guid? CoverMediaAssetId,
    string? VirtualTourUrl,
    ContentWorkflowStatus WorkflowStatus,
    DateTimeOffset? PublishAt,
    DateTimeOffset? PublishedAt,
    int SortOrder,
    bool IsFeatured,
    DateTimeOffset CreatedAt,
    byte[] RowVersion);

public sealed record ShowroomDetailDto(
    Guid Id,
    Guid? CoverMediaAssetId,
    string? VirtualTourUrl,
    string? EmbedCode,
    ContentWorkflowStatus WorkflowStatus,
    DateTimeOffset? PublishAt,
    DateTimeOffset? PublishedAt,
    int SortOrder,
    bool IsFeatured,
    DateTimeOffset CreatedAt,
    byte[] RowVersion,
    IReadOnlyList<ShowroomTranslationDto> Translations,
    IReadOnlyList<ShowroomMediaDto> Media,
    IReadOnlyList<ShowroomHotspotDto> Hotspots);

public sealed record BannerTranslationDto(
    string LanguageCode,
    string? Title,
    string? Subtitle,
    string? Description,
    string? CtaText,
    string? AccessibleLabel);

public sealed record BannerGroupDto(
    Guid Id,
    string Code,
    string Name,
    BannerPlacement Placement,
    bool IsActive,
    DateTimeOffset CreatedAt,
    byte[] RowVersion);

public sealed record BannerListItemDto(
    Guid Id,
    Guid BannerGroupId,
    BannerPlacement Placement,
    Guid DesktopMediaAssetId,
    Guid? MobileMediaAssetId,
    string? LinkUrl,
    BannerLinkTarget LinkTarget,
    ContentWorkflowStatus WorkflowStatus,
    DateTimeOffset? PublishAt,
    DateTimeOffset? PublishEndAt,
    int SortOrder,
    bool IsActive,
    DateTimeOffset CreatedAt,
    byte[] RowVersion);

public sealed record BannerDetailDto(
    Guid Id,
    Guid BannerGroupId,
    Guid DesktopMediaAssetId,
    Guid? MobileMediaAssetId,
    string? LinkUrl,
    BannerLinkTarget LinkTarget,
    ContentWorkflowStatus WorkflowStatus,
    DateTimeOffset? PublishAt,
    DateTimeOffset? PublishEndAt,
    int SortOrder,
    bool IsActive,
    DateTimeOffset CreatedAt,
    byte[] RowVersion,
    IReadOnlyList<BannerTranslationDto> Translations);

public sealed record SiteSettingDto(
    Guid Id,
    string Key,
    SiteSettingCategory Category,
    string? ValueJson,
    SiteSettingDataType DataType,
    bool IsPublic,
    bool IsSensitive,
    bool IsConfigured,
    DateTimeOffset UpdatedAt,
    byte[] RowVersion);

public sealed record FooterLinkDto(
    Guid Id,
    Guid FooterColumnId,
    string LabelTr,
    string LabelEn,
    string Url,
    BannerLinkTarget LinkTarget,
    int SortOrder,
    bool IsActive,
    byte[] RowVersion);

public sealed record FooterColumnDto(
    Guid Id,
    string Code,
    string TitleTr,
    string TitleEn,
    int SortOrder,
    bool IsActive,
    byte[] RowVersion,
    IReadOnlyList<FooterLinkDto> Links);

public sealed record PublicSiteSettingsDto(
    IReadOnlyDictionary<string, object?> Branding,
    IReadOnlyDictionary<string, object?> Contact,
    IReadOnlyDictionary<string, object?> SocialMedia,
    IReadOnlyDictionary<string, object?> Footer,
    IReadOnlyDictionary<string, object?> Seo,
    IReadOnlyDictionary<string, object?> Catalog,
    IReadOnlyDictionary<string, object?> Quote,
    IReadOnlyDictionary<string, object?> General);

public sealed record CmsRevisionListItemDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    int VersionNumber,
    CmsRevisionChangeType ChangeType,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string? Reason,
    int SchemaVersion);

public sealed record CmsRevisionDetailDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    int VersionNumber,
    CmsRevisionChangeType ChangeType,
    string SnapshotJson,
    string? ChangedFieldsJson,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string? Reason,
    string? CorrelationId,
    int SchemaVersion);

public sealed record PreviewTokenResultDto(
    string PreviewToken,
    DateTimeOffset ExpiresAt,
    string PreviewUrl,
    string EntityType,
    Guid EntityId,
    string LanguageCode);

public sealed record ReorderCmsItemDto(Guid Id, int SortOrder);

public sealed record EmbedSanitizationResult(bool IsAllowed, string? SanitizedHtml, string? ErrorMessage);

public sealed record CmsCacheInvalidationRequest(
    bool References = false,
    bool Showrooms = false,
    bool Banners = false,
    bool CustomerLogos = false,
    bool Settings = false,
    bool Footer = false,
    bool Sitemap = false);

#endregion

#region Commands & Requests

public sealed record CreateReferenceProjectCommand(
    string CustomerName,
    DateTimeOffset? ProjectDate,
    string? Location,
    Guid? CoverMediaAssetId,
    bool IsFeatured,
    IReadOnlyList<ReferenceProjectTranslationDto> Translations,
    Guid ActorUserId);

public sealed record UpdateReferenceProjectCommand(
    Guid Id,
    string CustomerName,
    DateTimeOffset? ProjectDate,
    string? Location,
    Guid? CoverMediaAssetId,
    bool IsFeatured,
    byte[] RowVersion,
    IReadOnlyList<ReferenceProjectTranslationDto> Translations,
    Guid ActorUserId);

public sealed record AddReferenceProjectMediaCommand(
    Guid ReferenceProjectId,
    Guid MediaAssetId,
    int SortOrder,
    bool IsCover,
    string? CaptionTr,
    string? CaptionEn);

public sealed record AddReferenceProjectProductCommand(
    Guid ReferenceProjectId,
    Guid ProductId,
    int SortOrder,
    string? Description);

public sealed record CreateCustomerLogoCommand(
    string Name,
    Guid MediaAssetId,
    string? WebsiteUrl,
    string? AltTextTr,
    string? AltTextEn,
    int SortOrder,
    bool IsActive,
    bool IsFeatured,
    Guid ActorUserId);

public sealed record UpdateCustomerLogoCommand(
    Guid Id,
    string Name,
    Guid MediaAssetId,
    string? WebsiteUrl,
    string? AltTextTr,
    string? AltTextEn,
    bool IsActive,
    bool IsFeatured,
    byte[] RowVersion,
    Guid ActorUserId);

public sealed record CreateShowroomCommand(
    Guid? CoverMediaAssetId,
    string? VirtualTourUrl,
    string? EmbedCode,
    bool IsFeatured,
    IReadOnlyList<ShowroomTranslationDto> Translations,
    Guid ActorUserId);

public sealed record UpdateShowroomCommand(
    Guid Id,
    Guid? CoverMediaAssetId,
    string? VirtualTourUrl,
    string? EmbedCode,
    bool IsFeatured,
    byte[] RowVersion,
    IReadOnlyList<ShowroomTranslationDto> Translations,
    Guid ActorUserId);

public sealed record CreateShowroomHotspotCommand(
    Guid ShowroomId,
    Guid? ProductId,
    string? TitleTr,
    string? TitleEn,
    string? DescriptionTr,
    string? DescriptionEn,
    double PositionX,
    double PositionY,
    double PositionZ,
    string SceneIdentifier,
    int SortOrder,
    bool IsActive);

public sealed record UpdateShowroomHotspotCommand(
    Guid HotspotId,
    Guid ShowroomId,
    Guid? ProductId,
    string? TitleTr,
    string? TitleEn,
    string? DescriptionTr,
    string? DescriptionEn,
    double PositionX,
    double PositionY,
    double PositionZ,
    string SceneIdentifier,
    bool IsActive,
    byte[] RowVersion);

public sealed record CreateBannerGroupCommand(
    string Code,
    string Name,
    BannerPlacement Placement,
    bool IsActive);

public sealed record CreateBannerCommand(
    Guid BannerGroupId,
    Guid DesktopMediaAssetId,
    Guid? MobileMediaAssetId,
    string? LinkUrl,
    BannerLinkTarget LinkTarget,
    DateTimeOffset? PublishAt,
    DateTimeOffset? PublishEndAt,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<BannerTranslationDto> Translations,
    Guid ActorUserId);

public sealed record UpdateBannerCommand(
    Guid Id,
    Guid DesktopMediaAssetId,
    Guid? MobileMediaAssetId,
    string? LinkUrl,
    BannerLinkTarget LinkTarget,
    DateTimeOffset? PublishAt,
    DateTimeOffset? PublishEndAt,
    bool IsActive,
    byte[] RowVersion,
    IReadOnlyList<BannerTranslationDto> Translations,
    Guid ActorUserId);

public sealed record UpdateSiteSettingItemDto(string Key, string? Value, byte[] RowVersion);

public sealed record UpdateSiteSettingsCommand(
    IReadOnlyList<UpdateSiteSettingItemDto> Settings,
    string? Reason,
    Guid ActorUserId);

public sealed record CreateFooterColumnCommand(
    string Code,
    string TitleTr,
    string TitleEn,
    int SortOrder,
    bool IsActive);

public sealed record UpdateFooterColumnCommand(
    Guid Id,
    string TitleTr,
    string TitleEn,
    bool IsActive,
    byte[] RowVersion);

public sealed record CreateFooterLinkCommand(
    Guid FooterColumnId,
    string LabelTr,
    string LabelEn,
    string Url,
    BannerLinkTarget LinkTarget,
    int SortOrder,
    bool IsActive);

public sealed record UpdateFooterLinkCommand(
    Guid Id,
    string LabelTr,
    string LabelEn,
    string Url,
    BannerLinkTarget LinkTarget,
    bool IsActive,
    byte[] RowVersion);

public sealed record ReorderCmsItemsCommand(IReadOnlyList<ReorderCmsItemDto> Items);

public sealed record CreatePreviewTokenCommand(string EntityType, Guid EntityId, string LanguageCode);

public sealed record RestoreCmsRevisionCommand(Guid RevisionId, string Reason, byte[] RowVersion, Guid ActorUserId);

#endregion

#region Interfaces

public interface IContentWorkflowService
{
    bool CanTransition(ContentWorkflowStatus currentStatus, ContentWorkflowStatus targetStatus);

    Task<ContentWorkflowResultDto> TransitionAsync(
        string contentType,
        Guid contentId,
        ContentWorkflowStatus targetStatus,
        DateTimeOffset? publishAt,
        string? reason,
        byte[] rowVersion,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public interface IReferenceProjectService
{
    Task<IReadOnlyList<ReferenceProjectListItemDto>> GetReferencesAsync(ContentWorkflowStatus? status = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<ReferenceProjectDetailDto?> GetReferenceByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ReferenceProjectDetailDto?> GetReferenceBySlugAsync(string slug, string languageCode, CancellationToken cancellationToken = default);
    Task<ReferenceProjectDetailDto> CreateReferenceAsync(CreateReferenceProjectCommand command, CancellationToken cancellationToken = default);
    Task<ReferenceProjectDetailDto> UpdateReferenceAsync(UpdateReferenceProjectCommand command, CancellationToken cancellationToken = default);
    Task AddMediaAsync(AddReferenceProjectMediaCommand command, CancellationToken cancellationToken = default);
    Task RemoveMediaAsync(Guid referenceId, Guid mediaAssetId, CancellationToken cancellationToken = default);
    Task ReorderMediaAsync(Guid referenceId, ReorderCmsItemsCommand command, CancellationToken cancellationToken = default);
    Task AddProductAsync(AddReferenceProjectProductCommand command, CancellationToken cancellationToken = default);
    Task RemoveProductAsync(Guid referenceId, Guid productId, CancellationToken cancellationToken = default);
    Task ReorderProductsAsync(Guid referenceId, ReorderCmsItemsCommand command, CancellationToken cancellationToken = default);
}

public interface ICustomerLogoService
{
    Task<IReadOnlyList<CustomerLogoDto>> GetLogosAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<CustomerLogoDto?> GetLogoByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerLogoDto> CreateLogoAsync(CreateCustomerLogoCommand command, CancellationToken cancellationToken = default);
    Task<CustomerLogoDto> UpdateLogoAsync(UpdateCustomerLogoCommand command, CancellationToken cancellationToken = default);
    Task ArchiveLogoAsync(Guid id, Guid actorUserId, CancellationToken cancellationToken = default);
    Task ReorderLogosAsync(ReorderCmsItemsCommand command, CancellationToken cancellationToken = default);
}

public interface IShowroomService
{
    Task<IReadOnlyList<ShowroomListItemDto>> GetShowroomsAsync(ContentWorkflowStatus? status = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<ShowroomDetailDto?> GetShowroomByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ShowroomDetailDto?> GetShowroomBySlugAsync(string slug, string languageCode, CancellationToken cancellationToken = default);
    Task<ShowroomDetailDto> CreateShowroomAsync(CreateShowroomCommand command, CancellationToken cancellationToken = default);
    Task<ShowroomDetailDto> UpdateShowroomAsync(UpdateShowroomCommand command, CancellationToken cancellationToken = default);
    Task AddMediaAsync(Guid showroomId, Guid mediaAssetId, int sortOrder, string? captionTr, string? captionEn, CancellationToken cancellationToken = default);
    Task RemoveMediaAsync(Guid showroomId, Guid mediaAssetId, CancellationToken cancellationToken = default);
    Task ReorderMediaAsync(Guid showroomId, ReorderCmsItemsCommand command, CancellationToken cancellationToken = default);
    Task<ShowroomHotspotDto> CreateHotspotAsync(CreateShowroomHotspotCommand command, CancellationToken cancellationToken = default);
    Task<ShowroomHotspotDto> UpdateHotspotAsync(UpdateShowroomHotspotCommand command, CancellationToken cancellationToken = default);
    Task DeleteHotspotAsync(Guid showroomId, Guid hotspotId, CancellationToken cancellationToken = default);
    Task ReorderHotspotsAsync(Guid showroomId, ReorderCmsItemsCommand command, CancellationToken cancellationToken = default);
}

public interface IBannerManagementService
{
    Task<IReadOnlyList<BannerGroupDto>> GetBannerGroupsAsync(CancellationToken cancellationToken = default);
    Task<BannerGroupDto> CreateBannerGroupAsync(CreateBannerGroupCommand command, CancellationToken cancellationToken = default);
    Task ArchiveBannerGroupAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BannerListItemDto>> GetBannersAsync(Guid? groupId = null, BannerPlacement? placement = null, ContentWorkflowStatus? status = null, CancellationToken cancellationToken = default);
    Task<BannerDetailDto?> GetBannerByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BannerDetailDto> CreateBannerAsync(CreateBannerCommand command, CancellationToken cancellationToken = default);
    Task<BannerDetailDto> UpdateBannerAsync(UpdateBannerCommand command, CancellationToken cancellationToken = default);
    Task ReorderBannersAsync(Guid groupId, ReorderCmsItemsCommand command, CancellationToken cancellationToken = default);
}

public interface ISiteSettingService
{
    Task<IReadOnlyList<SiteSettingDto>> GetAllSettingsAsync(SiteSettingCategory? category = null, bool? isPublic = null, CancellationToken cancellationToken = default);
    Task UpdateSettingsAsync(UpdateSiteSettingsCommand command, CancellationToken cancellationToken = default);
}

public interface IPublicSiteSettingService
{
    Task<PublicSiteSettingsDto> GetPublicSettingsAsync(CancellationToken cancellationToken = default);
}

public interface IFooterManagementService
{
    Task<IReadOnlyList<FooterColumnDto>> GetFooterColumnsAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<FooterColumnDto> CreateColumnAsync(CreateFooterColumnCommand command, CancellationToken cancellationToken = default);
    Task<FooterColumnDto> UpdateColumnAsync(UpdateFooterColumnCommand command, CancellationToken cancellationToken = default);
    Task DeleteColumnAsync(Guid id, CancellationToken cancellationToken = default);
    Task ReorderColumnsAsync(ReorderCmsItemsCommand command, CancellationToken cancellationToken = default);
    Task<FooterLinkDto> CreateLinkAsync(CreateFooterLinkCommand command, CancellationToken cancellationToken = default);
    Task<FooterLinkDto> UpdateLinkAsync(UpdateFooterLinkCommand command, CancellationToken cancellationToken = default);
    Task DeleteLinkAsync(Guid linkId, CancellationToken cancellationToken = default);
    Task ReorderLinksAsync(Guid columnId, ReorderCmsItemsCommand command, CancellationToken cancellationToken = default);
}

public interface ICmsRevisionService
{
    Task<CmsRevision> CreateRevisionAsync(
        string entityType,
        Guid entityId,
        CmsRevisionChangeType changeType,
        Guid actorUserId,
        string? reason = null,
        string? changedFieldsJson = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CmsRevisionListItemDto>> GetRevisionsAsync(string entityType, Guid entityId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<CmsRevisionDetailDto?> GetRevisionAsync(string entityType, Guid entityId, Guid revisionId, CancellationToken cancellationToken = default);
}

public interface ICmsSnapshotSerializer
{
    string Serialize<T>(T entity);
    T Deserialize<T>(string json);
}

public interface ICmsRevisionRestoreService
{
    Task<ContentWorkflowResultDto> RestoreRevisionAsync(string entityType, Guid entityId, Guid revisionId, string reason, byte[] rowVersion, Guid actorUserId, CancellationToken cancellationToken = default);
}

public interface ICmsPreviewTokenService
{
    Task<PreviewTokenResultDto> CreatePreviewTokenAsync(CreatePreviewTokenCommand command, CancellationToken cancellationToken = default);
    Task<bool> ValidatePreviewTokenAsync(string token, string entityType, Guid entityId, CancellationToken cancellationToken = default);
}

public interface IEmbedSanitizationService
{
    EmbedSanitizationResult Sanitize(string? embedCode);
}

public interface IAllowedEmbedProviderService
{
    bool IsAllowed(Uri uri);
}

public interface IUrlSafetyService
{
    bool IsSafeUrl(string? url);
}

public interface ICmsCacheInvalidationService
{
    Task InvalidateAsync(CmsCacheInvalidationRequest request, CancellationToken cancellationToken = default);
}

public interface ICmsScheduledPublishingService
{
    Task ExecuteScheduledPublishingAsync(CancellationToken cancellationToken = default);
    Task ExecuteBannerExpirationAsync(CancellationToken cancellationToken = default);
}

#endregion
