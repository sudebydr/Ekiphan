using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.Common;
using Ekiphan.Domain.Media;
using Ekiphan.Domain.Quotes;
using Ekiphan.Domain.DataImport;
using Ekiphan.Domain.Identity;
using Ekiphan.Domain.Content;
using Ekiphan.Domain.Seo;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Persistence;

public sealed class EkiphanDbContext(DbContextOptions<EkiphanDbContext> options)
    : DbContext(options)
{
    public DbSet<ProductSection> ProductSections => Set<ProductSection>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductRevision> ProductRevisions => Set<ProductRevision>();

    public DbSet<ProductBulkOperation> ProductBulkOperations => Set<ProductBulkOperation>();

    public DbSet<ProductBulkOperationItem> ProductBulkOperationItems => Set<ProductBulkOperationItem>();

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<ProductTag> ProductTags => Set<ProductTag>();

    public DbSet<AttributeDefinition> Attributes => Set<AttributeDefinition>();

    public DbSet<UnitDefinition> Units => Set<UnitDefinition>();

    public DbSet<CategoryAttributeAssignment> CategoryAttributes =>
        Set<CategoryAttributeAssignment>();

    public DbSet<ProductAttributeValue> ProductAttributeValues =>
        Set<ProductAttributeValue>();

    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    public DbSet<ProductRelation> ProductRelations => Set<ProductRelation>();
    public DbSet<PendingProductRelation> PendingProductRelations => Set<PendingProductRelation>();

    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<MediaVariant> MediaVariants => Set<MediaVariant>();

    public DbSet<ProductMedia> ProductMedia => Set<ProductMedia>();

    public DbSet<ProductMediaImportBatch> ProductMediaImportBatches => Set<ProductMediaImportBatch>();

    public DbSet<ProductMediaImportBatchItem> ProductMediaImportBatchItems => Set<ProductMediaImportBatchItem>();

    public DbSet<CategoryMedia> CategoryMedia => Set<CategoryMedia>();

    public DbSet<BrandMedia> BrandMedia => Set<BrandMedia>();

    public DbSet<QuoteRequest> QuoteRequests => Set<QuoteRequest>();

    public DbSet<QuoteInternalNote> QuoteInternalNotes => Set<QuoteInternalNote>();

    public DbSet<QuoteActivity> QuoteActivities => Set<QuoteActivity>();

    public DbSet<EmailQueueItem> EmailQueueItems => Set<EmailQueueItem>();

    public DbSet<QuoteSpamAssessment> QuoteSpamAssessments => Set<QuoteSpamAssessment>();

    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();

    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();

    public DbSet<ImportBatchItem> ImportBatchItems => Set<ImportBatchItem>();

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    public DbSet<AdminSession> AdminSessions => Set<AdminSession>();
    public DbSet<AdminRole> AdminRoles => Set<AdminRole>();
    public DbSet<PermissionDefinition> AdminPermissions => Set<PermissionDefinition>();
    public DbSet<AdminUserRole> AdminUserRoles => Set<AdminUserRole>();
    public DbSet<RolePermissionGrant> AdminRolePermissions => Set<RolePermissionGrant>();
    public DbSet<AdminUserPermissionOverride> AdminUserPermissionOverrides => Set<AdminUserPermissionOverride>();
    public DbSet<TwoFactorRecoveryCode> TwoFactorRecoveryCodes => Set<TwoFactorRecoveryCode>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<TemporarySecurityToken> TemporarySecurityTokens => Set<TemporarySecurityToken>();
    public DbSet<AdminLoginAttempt> AdminLoginAttempts => Set<AdminLoginAttempt>();
    public DbSet<AuthorizationAuditLog> AuthorizationAuditLogs => Set<AuthorizationAuditLog>();
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
    public DbSet<SecurityNotificationOutbox> SecurityNotificationOutbox => Set<SecurityNotificationOutbox>();

    public DbSet<ContentPage> ContentPages => Set<ContentPage>();

    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    public DbSet<HomepageHero> HomepageHeroes => Set<HomepageHero>();

    public DbSet<GalleryItem> GalleryItems => Set<GalleryItem>();

    public DbSet<PressRelease> PressReleases => Set<PressRelease>();

    public DbSet<ContactRequest> ContactRequests => Set<ContactRequest>();

    public DbSet<ContactRequestNote> ContactRequestNotes =>
        Set<ContactRequestNote>();

    public DbSet<ContactReason> ContactReasons => Set<ContactReason>();

    public DbSet<ComplaintCategory> ComplaintCategories =>
        Set<ComplaintCategory>();

    public DbSet<ReferenceProject> ReferenceProjects => Set<ReferenceProject>();
    public DbSet<ReferenceProjectTranslation> ReferenceProjectTranslations => Set<ReferenceProjectTranslation>();
    public DbSet<ReferenceProjectMedia> ReferenceProjectMedia => Set<ReferenceProjectMedia>();
    public DbSet<ReferenceProjectProduct> ReferenceProjectProducts => Set<ReferenceProjectProduct>();
    public DbSet<CustomerLogo> CustomerLogos => Set<CustomerLogo>();
    public DbSet<Showroom> Showrooms => Set<Showroom>();
    public DbSet<ShowroomTranslation> ShowroomTranslations => Set<ShowroomTranslation>();
    public DbSet<ShowroomMedia> ShowroomMedia => Set<ShowroomMedia>();
    public DbSet<ShowroomHotspot> ShowroomHotspots => Set<ShowroomHotspot>();
    public DbSet<BannerGroup> BannerGroups => Set<BannerGroup>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<BannerTranslation> BannerTranslations => Set<BannerTranslation>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<FooterColumn> FooterColumns => Set<FooterColumn>();
    public DbSet<FooterLink> FooterLinks => Set<FooterLink>();
    public DbSet<CmsRevision> CmsRevisions => Set<CmsRevision>();

    public DbSet<RobotsConfiguration> RobotsConfigurations => Set<RobotsConfiguration>();
    public DbSet<RedirectRule> RedirectRules => Set<RedirectRule>();
    public DbSet<BrokenLinkScan> BrokenLinkScans => Set<BrokenLinkScan>();
    public DbSet<BrokenLinkResult> BrokenLinkResults => Set<BrokenLinkResult>();
    public DbSet<SeoRevision> SeoRevisions => Set<SeoRevision>();
    public DbSet<SeoQualitySnapshot> SeoQualitySnapshots => Set<SeoQualitySnapshot>();
    public DbSet<SeoImportBatch> SeoImportBatches => Set<SeoImportBatch>();
    public DbSet<SeoImportBatchItem> SeoImportBatchItems => Set<SeoImportBatchItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(AssemblyReference.Assembly);
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditTimestamps();
        return base.SaveChanges();
    }

    private void ApplyAuditTimestamps()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(entity => entity.CreatedAt).CurrentValue = now;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Property(entity => entity.UpdatedAt).CurrentValue = now;
            }
        }
    }
}
