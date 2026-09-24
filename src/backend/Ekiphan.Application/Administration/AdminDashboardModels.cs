using Ekiphan.Domain.Identity;

namespace Ekiphan.Application.Administration;

public sealed record AdminDashboardAccess(
    bool Catalog,
    bool Quotes,
    bool Contacts,
    bool Imports,
    bool Media,
    bool Content)
{
    public static AdminDashboardAccess FromPermissions(
        IEnumerable<string> permissions)
    {
        var values = permissions.ToHashSet(StringComparer.Ordinal);
        return new AdminDashboardAccess(
            values.Contains(AdminPermissionCode.CatalogManage),
            values.Contains(AdminPermissionCode.QuotesRead) ||
                values.Contains(AdminPermissionCode.QuotesManage),
            values.Contains(AdminPermissionCode.ContactsRead) ||
                values.Contains(AdminPermissionCode.ContactsManage),
            values.Contains(AdminPermissionCode.ImportsManage) ||
                values.Contains(AdminPermissionCode.ImportsPublish),
            values.Contains(AdminPermissionCode.MediaManage),
            values.Contains(AdminPermissionCode.ContentManage));
    }
}

public sealed record AdminProductMetrics(
    int Total,
    int Published,
    int Unpublished,
    int MissingGalleryImage,
    int MissingTurkishTranslation,
    int MissingEnglishTranslation);

public sealed record AdminQuoteMetrics(
    int New,
    int InProgress,
    int Total);

public sealed record AdminDashboardProduct(
    Guid Id,
    string SKU,
    string Name,
    bool IsPublished,
    DateTimeOffset UpdatedAt);

public sealed record AdminDashboardQuote(
    Guid Id,
    string RequestNumber,
    string CompanyName,
    string Status,
    int ItemCount,
    DateTimeOffset CreatedAt);

public sealed record AdminLatestImport(
    Guid Id,
    string Status,
    string SourceType,
    string? OriginalFileName,
    int TotalRowCount,
    int InvalidRowCount,
    int WarningCount,
    DateTimeOffset CreatedAt);

public sealed record AdminDashboardSystemStatus(
    bool ApiOperational,
    bool DatabaseReachable,
    long DatabaseLatencyMilliseconds);

public sealed record AdminDashboard(
    DateTimeOffset GeneratedAt,
    AdminProductMetrics? Products,
    int? TotalCategories,
    int? TotalBrands,
    AdminQuoteMetrics? Quotes,
    int? NewContactRequests,
    int? TotalMediaFiles,
    int? ContentUpdatesLast30Days,
    IReadOnlyList<AdminDashboardQuote>? RecentQuotes,
    IReadOnlyList<AdminDashboardProduct>? RecentProducts,
    IReadOnlyList<AdminDashboardProduct>? ProductsMissingImages,
    IReadOnlyList<AdminDashboardProduct>? ProductsMissingEnglishContent,
    AdminLatestImport? LatestImport,
    AdminDashboardSystemStatus System,
    IReadOnlyList<string> Warnings);

public interface IAdminDashboardService
{
    Task<AdminDashboard> GetAsync(
        AdminDashboardAccess access,
        CancellationToken cancellationToken = default);
}
