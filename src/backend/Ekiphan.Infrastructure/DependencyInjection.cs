using Ekiphan.Application.Administration;
using Ekiphan.Application.Catalog;
using Ekiphan.Application.Deployments;
using Ekiphan.Application.DataImport;
using Ekiphan.Application.Quotes;
using Ekiphan.Application.Media;
using Ekiphan.Application.Identity;
using Ekiphan.Application.Content;
using Ekiphan.Infrastructure.Administration;
using Ekiphan.Infrastructure.Catalog;
using Ekiphan.Infrastructure.Deployments;
using Ekiphan.Infrastructure.DataImport;
using Ekiphan.Infrastructure.Quotes;
using Ekiphan.Infrastructure.Media;
using Ekiphan.Infrastructure.Identity;
using Ekiphan.Infrastructure.Content;
using Ekiphan.Infrastructure.Content.Background;
using Ekiphan.Application.Seo;
using Ekiphan.Application.MediaImport;
using Ekiphan.Infrastructure.Seo;
using Ekiphan.Infrastructure.MediaImport;
using Ekiphan.Infrastructure.CatalogPdfImport;
using Ekiphan.Application.CatalogPdfImport;
using Ekiphan.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using FluentValidation;
using Ekiphan.Application.Caching;
using Ekiphan.Infrastructure.Caching;
using Ekiphan.Application.Metrics;
using Ekiphan.Infrastructure.Metrics;
using Ekiphan.Application.Monitoring;
using Ekiphan.Infrastructure.Monitoring;
using Ekiphan.Application.Seed;
using Ekiphan.Infrastructure.Seed;
using Ekiphan.Application.Warmup;
using Ekiphan.Infrastructure.Warmup;
using StackExchange.Redis;
namespace Ekiphan.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string? redisConnectionString)
    {
        services.AddSingleton<IConfigurationValidationService, ConfigurationValidationService>();
        services.AddDataProtection().SetApplicationName("Ekiphan.Admin.Security");
        services.AddMemoryCache();
        services.AddOptions<AdminSecurityOptions>().Bind(configuration.GetSection(AdminSecurityOptions.SectionName))
            .Validate(x=>x.TwoFactorSetupTokenLifetimeMinutes is >0 and <=30&&x.TwoFactorLoginTokenLifetimeMinutes is >0 and <=10&&x.TwoFactorMaxAttempts is >=3 and <=10&&x.TwoFactorLockoutMinutes is >=5 and <=60&&x.TotpAllowedTimeStepDrift is >=0 and <=1&&x.RecoveryCodeCount==10,"Admin security configuration is invalid.").ValidateOnStart();
        var connectionString = configuration.GetConnectionString("EkiphanDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'EkiphanDatabase' is required.");
        }

        services.AddSingleton<EfCoreSlowQueryInterceptor>();
        services.AddDbContext<EkiphanDbContext>(
            (sp, options) => {
                var interceptor = sp.GetRequiredService<EfCoreSlowQueryInterceptor>();
                options.UseSqlServer(
                    connectionString,
                    sqlOptions => sqlOptions.MigrationsAssembly(
                        AssemblyReference.Assembly.FullName))
                    .AddInterceptors(interceptor);
            });
        services.AddScoped<ICatalogQueryService, CatalogQueryService>();
        services.AddSingleton<IPublicMediaUrlResolver>(
            new PublicMediaUrlResolver(
                configuration["PublicMedia:BaseUrl"]));
        services.AddSingleton<IMediaFileSignatureValidator,
            MediaFileSignatureValidator>();
        services.AddSingleton<IMediaThreatScanner,
            NoOpMediaThreatScanner>();
        var mediaPublicBaseUrl = configuration["Storage:CdnBaseUrl"] ??
            configuration["PublicMedia:BaseUrl"];
        var mediaStorageProvider = configuration["MediaStorage:Provider"]?.Trim();
        services.AddSingleton<IMediaFileStorage>(mediaStorageProvider?.ToUpperInvariant() switch
        {
            "LOCAL" => new LocalMediaFileStorage(
                configuration["MediaStorage:LocalRoot"],
                mediaPublicBaseUrl),
            "S3" => new S3MediaFileStorage(
                S3MediaStorageOptions.FromConfiguration(configuration),
                mediaPublicBaseUrl),
            null or "" => throw new InvalidOperationException(
                "MediaStorage:Provider must be configured as 'Local' or 'S3'."),
            _ => throw new InvalidOperationException(
                $"Unsupported MediaStorage:Provider '{mediaStorageProvider}'. Supported values are 'Local' and 'S3'."),
        });
        services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
        services.AddScoped<MediaUploadService>();
        services.AddOptions<MediaProcessingOptions>()
            .Bind(configuration.GetSection(MediaProcessingOptions.SectionName))
            .Validate(x => x.MaxUploadSizeMb > 0 && x.MaxWidth > 0 && x.MaxHeight > 0 &&
                x.MaxPixelCount > 0 && x.DefaultWebPQuality is >= 1 and <= 100 &&
                x.MinimumWebPQuality is >= 1 and <= 100 &&
                x.MinimumWebPQuality <= x.DefaultWebPQuality && x.TargetFileSizeKb > 0 &&
                x.ProcessingTimeoutSeconds > 0 && x.MaxConcurrentJobs > 0,
                "Media processing configuration is invalid.")
            .ValidateOnStart();
        services.AddSingleton<IValidator<MediaUploadCommand>>(provider =>
            new MediaUploadCommandValidator(provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MediaProcessingOptions>>().Value));
        services.AddTransient<IWebPOptimizationService, AdaptiveWebPOptimizationService>();
        services.AddTransient<IMediaImageDecoder, ImageSharpMediaImageDecoder>();
        services.AddTransient<IMediaMetadataSanitizer, ImageSharpMediaMetadataSanitizer>();
        services.AddTransient<IMediaVariantGenerator, ImageSharpMediaVariantGenerator>();
        services.AddScoped<IMediaProcessingRepository, MediaProcessingRepository>();
        services.AddScoped<IMediaProcessingService, MediaProcessingService>();
        services.AddScoped<
            IAdminProductRelationService,
            AdminProductRelationService>();
        services.AddScoped<
            IAdminProductManagementService,
            AdminProductManagementService>();
        services.AddScoped<IAdminBrandService, AdminBrandService>();
        services.AddScoped<IAdminCategoryService, AdminCategoryService>();
        services.AddScoped<IAdminAttributeService, AdminAttributeService>();
        services.AddScoped<IAdminVariantService, AdminVariantService>();
        services.AddScoped<IAdminMediaService, AdminMediaService>();
        services.AddScoped<IAdminDictionaryService, AdminDictionaryService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<
            IAdminAuthenticationService,
            AdminAuthenticationService>();
        services.AddScoped<
            IAdminUserManagementService,
            AdminUserManagementService>();
        services.AddScoped<IUserPermissionService, UserPermissionService>();
        services.AddSingleton<ISecurityTokenHasher,Sha256SecurityTokenHasher>();
        services.AddSingleton<ITwoFactorService,TotpTwoFactorService>();
        services.AddScoped<ITwoFactorTokenService,TwoFactorTokenService>();
        services.AddScoped<IRecoveryCodeService,SecureRecoveryCodeService>();
        services.AddScoped<IUserSecurityService,UserSecurityService>();
        services.AddScoped<IAdminSessionService,AdminSessionService>();
        services.AddScoped<ILoginAttemptService,LoginAttemptService>();
        services.AddScoped<IPasswordSecurityService,PasswordSecurityService>();
        services.AddScoped<IReAuthenticationService,ReAuthenticationService>();
        services.AddScoped<ISecurityNotificationService,SecurityNotificationService>();
        services.AddScoped<IPasswordResetService,PasswordResetService>();
        services.AddSingleton<IAuditValueSanitizer,AuditValueSanitizer>();
        services.AddScoped<IAuditLogService,AuditLogService>();
        services.AddSingleton<ISecurityRiskEvaluator,DefaultSecurityRiskEvaluator>();
        services.AddScoped<ISecurityEventService,SecurityEventService>();
        services.AddScoped<IValidator<ConfirmTwoFactorCommand>,ConfirmTwoFactorCommandValidator>();
        services.AddScoped<IValidator<VerifyTwoFactorLoginCommand>,VerifyTwoFactorLoginCommandValidator>();
        services.AddScoped<IValidator<DisableTwoFactorCommand>,DisableTwoFactorCommandValidator>();
        services.AddScoped<IValidator<ChangePasswordCommand>,ChangePasswordCommandValidator>();
        services.AddScoped<IValidator<ForgotPasswordCommand>,ForgotPasswordCommandValidator>();
        services.AddScoped<IValidator<ResetPasswordCommand>,ResetPasswordCommandValidator>();
        services.AddScoped<IValidator<ReAuthenticateCommand>,ReAuthenticateCommandValidator>();
        services.AddScoped<IRolePermissionManagementService, RolePermissionManagementService>();
        services.AddScoped<IValidator<CreateRoleCommand>, CreateRoleCommandValidator>();
        services.AddScoped<IValidator<UpdateRoleCommand>, UpdateRoleCommandValidator>();
        services.AddScoped<IValidator<AssignUserRolesCommand>, AssignUserRolesCommandValidator>();
        services.AddScoped<IValidator<AddUserPermissionOverrideCommand>>(provider =>
            new AddUserPermissionOverrideCommandValidator(provider.GetRequiredService<TimeProvider>()));
        services.AddScoped<IContentPageService, ContentPageService>();
        services.AddScoped<IAdminSeoService, AdminSeoService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<IHomepageHeroService, HomepageHeroService>();
        services.AddScoped<IGalleryService, GalleryService>();
        services.AddScoped<IPressReleaseService, PressReleaseService>();
        services.AddScoped<IContactRequestService, ContactRequestService>();
        services.AddScoped<IContactTaxonomyService, ContactTaxonomyService>();
        services.AddScoped<
            IPasswordHasher<AdminUser>,
            PasswordHasher<AdminUser>>();
        services.AddScoped<IQuoteSubmissionRepository, QuoteSubmissionRepository>();
        services.AddScoped<IAdminQuoteQueryService, AdminQuoteQueryService>();
        services.AddScoped<IQuoteManagementRepository, QuoteManagementRepository>();
        services.AddSingleton<
            IQuoteRequestNumberGenerator,
            QuoteRequestNumberGenerator>();
        services.AddScoped<QuoteSubmissionService>();
        services.AddScoped<QuoteManagementService>();
        services.AddSingleton<ITabularImportFileReader, TabularImportFileReader>();
        services.AddScoped<IImportJobRepository, ImportJobRepository>();
        services.AddScoped<IImportQueryService, ImportQueryService>();
        services.AddScoped<IImportIssueReportSource, ImportIssueReportSource>();
        services.AddScoped<IImportIssueReportWriter, ImportIssueCsvWriter>();
        services.AddScoped<IImportPublishingRepository, ImportPublishingRepository>();
        services.AddScoped<ImportPublishingService>();
        services.AddScoped<IImportReferenceResolver, ImportReferenceResolver>();
        services.AddScoped<ImportStagingService>();
        services.AddSingleton<IProductImportMappingService, ProductImportMappingService>();
        services.AddSingleton<IProductImportUploadStore, ProductImportUploadStore>();
        services.AddSingleton<IProductImportFileReader, CsvProductImportFileReader>();
        services.AddSingleton<IProductImportFileReader, XlsxProductImportFileReader>();
        services.AddScoped<IProductImportRepository, ProductImportRepository>();
        services.AddScoped<IProductImportService, ProductImportService>();
        services.AddScoped<IProductImportValidationService, ProductImportValidationService>();
        services.AddScoped<IProductImportExecutionService, ProductImportExecutionService>();
        services.AddScoped<IProductImportRollbackService, ProductImportRollbackService>();
        services.AddSingleton<IValidator<ProductImportPreviewRequest>, ProductImportPreviewRequestValidator>();
        services.AddSingleton<IValidator<ProductImportValidateCommand>, ProductImportValidateCommandValidator>();
        services.AddSingleton<IValidator<ProductImportExecuteCommand>, ProductImportExecuteCommandValidator>();
        services.AddSingleton<IValidator<ProductImportRollbackCommand>, ProductImportRollbackCommandValidator>();
        services.AddOptions<ProductMediaImportOptions>()
            .Bind(configuration.GetSection(ProductMediaImportOptions.SectionName))
            .Validate(x => x.MaxZipSizeMb > 0 && x.MaxFileCount > 0 && x.MaxExtractedSizeMb > 0 &&
                x.MaxSingleImageSizeMb > 0 && x.MaxCompressionRatio > 0 && x.UploadTokenLifetimeMinutes > 0 &&
                x.ValidationTokenLifetimeMinutes > 0 && x.TemporaryFileLifetimeMinutes > 0,
                "ProductMediaImport limits must be positive.")
            .ValidateOnStart();
        services.AddSingleton<IProductMediaSkuParser, ProductMediaSkuParser>();
        services.AddSingleton<IProductMediaSkuResolver, ProductMediaSkuResolver>();
        services.AddSingleton<IProductMediaDuplicateDetector, Sha256ProductMediaDuplicateDetector>();
        services.AddSingleton<ITemporaryProductMediaStorage, TemporaryProductMediaStorage>();
        services.AddSingleton<IProductMediaImportTokenService, ProductMediaImportTokenService>();
        services.AddSingleton<IProductMediaImportArchiveReader, ZipProductMediaImportArchiveReader>();
        services.AddScoped<IProductMediaImportRepository, ProductMediaImportRepository>();
        services.AddScoped<IProductMediaImportService, ProductMediaImportService>();
        services.AddScoped<IProductMediaImportValidationService, ProductMediaImportValidationService>();
        services.AddScoped<IProductMediaImportExecutionService, ProductMediaImportExecutionService>();
        services.AddScoped<IProductMediaImportRollbackService, ProductMediaImportRollbackService>();
        services.AddScoped<ICatalogPdfImportService, CatalogPdfImportService>();
        services.AddSingleton<IValidator<ProductMediaImportPreviewRequest>, ProductMediaImportPreviewRequestValidator>();
        services.AddSingleton<IValidator<ProductMediaImportValidateCommand>, ProductMediaImportValidateCommandValidator>();
        services.AddSingleton<IValidator<ProductMediaImportExecuteCommand>, ProductMediaImportExecuteCommandValidator>();
        services.AddSingleton<IValidator<ProductMediaImportRollbackCommand>, ProductMediaImportRollbackCommandValidator>();
        services.Configure<QuoteEmailOptions>(configuration.GetSection(QuoteEmailOptions.SectionName));
        services.Configure<BotProtectionOptions>(configuration.GetSection(BotProtectionOptions.SectionName));
        services.Configure<QuoteRetentionOptions>(configuration.GetSection(QuoteRetentionOptions.SectionName));

        services.AddSingleton<IQuoteStatusTransitionService, QuoteStatusTransitionService>();
        services.AddScoped<IQuoteActivityService, QuoteActivityService>();
        services.AddScoped<IQuoteExportService, QuoteExportService>();
        services.AddScoped<IQuotePdfGenerator, QuotePdfGenerator>();
        services.AddScoped<IBotVerificationService, BotVerificationService>();
        services.AddScoped<IQuoteSpamDetectionService, QuoteSpamDetectionService>();
        services.AddScoped<IQuoteNotificationService, QuoteNotificationService>();
        services.AddScoped<IQuoteEmailQueueService, QuoteEmailQueueService>();
        services.AddScoped<IQuoteRetentionService, QuoteRetentionService>();
        services.AddScoped<IQuoteDeletionService, QuoteDeletionService>();

        services.AddHostedService<Ekiphan.Infrastructure.Quotes.Background.QuoteEmailQueueWorker>();
        services.AddHostedService<Ekiphan.Infrastructure.Quotes.Background.QuoteRetentionJob>();

        services.AddScoped<IValidator<AssignQuoteCommand>, AssignQuoteCommandValidator>();
        services.AddScoped<IValidator<ChangeQuoteStatusCommand>, ChangeQuoteStatusCommandValidator>();
        services.AddScoped<IValidator<AddQuoteNoteCommand>, AddQuoteNoteCommandValidator>();
        services.AddScoped<IValidator<UpdateQuoteNoteCommand>, UpdateQuoteNoteCommandValidator>();
        services.AddScoped<IValidator<ArchiveQuoteCommand>, ArchiveQuoteCommandValidator>();
        services.AddScoped<IValidator<PermanentDeleteQuoteCommand>, PermanentDeleteQuoteCommandValidator>();
        services.AddScoped<IValidator<SubmitQuoteCommand>, SubmitQuoteCommandValidator>();

        // Advanced Product Management Registrations
        services.AddSingleton<IProductSnapshotSerializer, ProductSnapshotSerializer>();
        services.AddScoped<IProductRevisionService, ProductRevisionService>();
        services.AddScoped<IProductRevisionRestoreService, ProductRevisionRestoreService>();
        services.AddScoped<IProductWorkflowService, ProductWorkflowService>();
        services.AddScoped<IProductDuplicationService, ProductDuplicationService>();
        services.AddSingleton<InMemoryProductBulkOperationQueue>();
        services.AddSingleton<IProductBulkOperationQueue>(sp => sp.GetRequiredService<InMemoryProductBulkOperationQueue>());
        services.AddScoped<IProductBulkOperationService, ProductBulkOperationService>();
        services.AddHostedService<Ekiphan.Infrastructure.Catalog.Background.ProductBulkOperationWorker>();

        services.AddScoped<IProductQualityRule, MissingSkuQualityRule>();
        services.AddScoped<IProductQualityRule, MissingProductNameQualityRule>();
        services.AddScoped<IProductQualityRule, MissingPrimaryImageQualityRule>();
        services.AddScoped<IProductQualityRule, MissingCategoryQualityRule>();
        services.AddScoped<IProductQualityRule, MissingBrandQualityRule>();
        services.AddScoped<IProductQualityRule, MissingEnglishTranslationQualityRule>();
        services.AddScoped<IProductQualityRule, MissingSeoTitleQualityRule>();
        services.AddScoped<IProductQualityRule, DuplicateSkuQualityRule>();
        services.AddScoped<IProductQualityRule, DuplicateSlugQualityRule>();
        services.AddScoped<IProductQualityRule, MissingRequiredAttributeQualityRule>();
        services.AddScoped<IProductQualityRule, InvalidMediaQualityRule>();
        services.AddScoped<IProductQualityService, ProductQualityService>();

        services.AddSingleton<IProductSkuNormalizationService, ProductSkuNormalizationService>();
        services.AddScoped<IProductSlugValidationService, ProductSlugValidationService>();
        services.AddScoped<ICategoryProductOrderingService, CategoryProductOrderingService>();
        services.AddScoped<IProductCacheInvalidationService, ProductCacheInvalidationService>();
        services.AddScoped<IProductSearchIndexService, ProductSearchIndexService>();

        services.AddScoped<IValidator<DuplicateProductCommand>, DuplicateProductCommandValidator>();
        services.AddScoped<IValidator<ProductBulkPreviewCommand>, ProductBulkPreviewCommandValidator>();
        services.AddScoped<IValidator<ProductBulkExecuteCommand>, ProductBulkExecuteCommandValidator>();
        services.AddScoped<IValidator<ReorderCategoryProductsCommand>, ReorderCategoryProductsCommandValidator>();

        // Advanced CMS Registrations
        services.AddSingleton<IUrlSafetyService, UrlSafetyService>();
        services.AddSingleton<ShowroomEmbedSanitizer>();
        services.AddSingleton<IEmbedSanitizationService>(sp => sp.GetRequiredService<ShowroomEmbedSanitizer>());
        services.AddSingleton<IAllowedEmbedProviderService>(sp => sp.GetRequiredService<ShowroomEmbedSanitizer>());
        services.AddSingleton<ICmsSnapshotSerializer, CmsSnapshotSerializer>();
        services.AddSingleton<ICmsPreviewTokenService, CmsPreviewTokenService>();
        services.AddScoped<ICmsCacheInvalidationService, CmsCacheInvalidationService>();
        services.AddScoped<ICmsRevisionService, CmsRevisionService>();
        services.AddScoped<ICmsRevisionRestoreService, CmsRevisionRestoreService>();
        services.AddScoped<IContentWorkflowService, ContentWorkflowService>();
        services.AddScoped<IReferenceProjectService, ReferenceProjectService>();
        services.AddScoped<ICustomerLogoService, CustomerLogoService>();
        services.AddScoped<IShowroomService, ShowroomService>();
        services.AddScoped<IBannerManagementService, BannerManagementService>();
        services.AddScoped<ISiteSettingService, SiteSettingService>();
        services.AddScoped<IPublicSiteSettingService, SiteSettingService>();
        services.AddScoped<IFooterManagementService, FooterManagementService>();
        services.AddSingleton<ICmsScheduledPublishingService, CmsScheduledPublishingService>();
        services.AddHostedService<Ekiphan.Infrastructure.Content.Background.CmsScheduledPublishingWorker>();

        services.AddScoped<IValidator<CreateReferenceProjectCommand>, CreateReferenceProjectCommandValidator>();
        services.AddScoped<IValidator<UpdateReferenceProjectCommand>, UpdateReferenceProjectCommandValidator>();
        services.AddScoped<IValidator<CreateCustomerLogoCommand>, CreateCustomerLogoCommandValidator>();
        services.AddScoped<IValidator<UpdateCustomerLogoCommand>, UpdateCustomerLogoCommandValidator>();
        services.AddScoped<IValidator<CreateShowroomCommand>, CreateShowroomCommandValidator>();
        services.AddScoped<IValidator<CreateShowroomHotspotCommand>, CreateShowroomHotspotCommandValidator>();
        services.AddScoped<IValidator<CreateBannerGroupCommand>, CreateBannerGroupCommandValidator>();
        services.AddScoped<IValidator<CreateBannerCommand>, CreateBannerCommandValidator>();
        services.AddScoped<IValidator<UpdateSiteSettingsCommand>, UpdateSiteSettingsCommandValidator>();
        services.AddScoped<IValidator<CreateFooterColumnCommand>, CreateFooterColumnCommandValidator>();
        services.AddScoped<IValidator<CreateFooterLinkCommand>, CreateFooterLinkCommandValidator>();
        services.AddScoped<IValidator<ReorderCmsItemsCommand>, ReorderCmsItemsCommandValidator>();

        // Centralized SEO Management Registrations
        services.AddScoped<ISeoDocumentProvider, Ekiphan.Infrastructure.Seo.Providers.ProductSeoDocumentProvider>();
        services.AddScoped<ISeoDocumentProvider, Ekiphan.Infrastructure.Seo.Providers.CategorySeoDocumentProvider>();
        services.AddScoped<ISeoDocumentProvider, Ekiphan.Infrastructure.Seo.Providers.BrandSeoDocumentProvider>();
        services.AddScoped<ISeoDocumentProvider, Ekiphan.Infrastructure.Seo.Providers.ContentPageSeoDocumentProvider>();
        services.AddScoped<ISeoDocumentProvider, Ekiphan.Infrastructure.Seo.Providers.NewsSeoDocumentProvider>();
        services.AddScoped<ISeoDocumentProvider, Ekiphan.Infrastructure.Seo.Providers.ReferenceProjectSeoDocumentProvider>();
        services.AddScoped<ISeoDocumentProvider, Ekiphan.Infrastructure.Seo.Providers.ShowroomSeoDocumentProvider>();

        services.AddScoped<ISitemapService, SitemapService>();
        services.AddScoped<ISeoCacheInvalidationService, SeoCacheInvalidationService>();
        services.AddScoped<IRobotsConfigurationService, RobotsConfigurationService>();
        services.AddScoped<IRedirectRuleService, RedirectService>();
        services.AddScoped<IRedirectResolver, RedirectService>();
        services.AddScoped<IRedirectLoopDetector, RedirectService>();
        services.AddScoped<ISeoRedirectCreationService, RedirectService>();

        services.AddSingleton<ISsrfUrlSafetyService, SsrfUrlSafetyService>();
        services.AddSingleton<IInternalLinkExtractor, InternalLinkExtractor>();
        services.AddHttpClient<IBrokenLinkScanner, BrokenLinkScanner>();

        services.AddScoped<ISeoQualityRule, Ekiphan.Infrastructure.Seo.QualityRules.MissingMetaTitleRule>();
        services.AddScoped<ISeoQualityRule, Ekiphan.Infrastructure.Seo.QualityRules.MissingMetaDescriptionRule>();
        services.AddScoped<ISeoQualityRule, Ekiphan.Infrastructure.Seo.QualityRules.DuplicateSlugRule>();
        services.AddScoped<ISeoQualityService, SeoQualityService>();

        services.AddScoped<ISchemaMarkupService, SchemaMarkupService>();
        services.AddScoped<ISeoImportExportService, SeoImportExportService>();
        services.AddScoped<ISeoRevisionService, SeoRevisionService>();

        services.AddSingleton(TimeProvider.System);

        // Performance & Caching
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<IApplicationCache, MemoryApplicationCache>();
            services.AddSingleton<IDistributedLockService, InMemoryDistributedLockService>();
        }
        else
        {
            services.AddSingleton<IConnectionMultiplexer>(
                _ => ConnectionMultiplexer.Connect(redisConnectionString));
            services.AddSingleton<IApplicationCache, RedisApplicationCache>();
            services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();
        }

        services.AddSingleton<ICacheKeyFactory, DefaultCacheKeyFactory>();
        services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
        services.AddSingleton<ICacheMetrics, CacheMetrics>();
        services.AddSingleton<IPerformanceMetrics, PerformanceMetrics>();
        services.AddScoped<IPerformanceTestDataSeeder, PerformanceTestDataSeeder>();
        services.AddHostedService<Ekiphan.Infrastructure.Warmup.Jobs.CacheWarmupJob>();
        services.AddHostedService<Ekiphan.Infrastructure.Warmup.Jobs.CacheMetadataCleanupJob>();
        services.AddScoped<ICacheWarmupService, CacheWarmupService>();
        services.AddSingleton<ISlowQueryMonitor, SlowQueryMonitor>();

        return services;
    }
}
