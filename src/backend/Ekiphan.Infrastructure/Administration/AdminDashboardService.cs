using Ekiphan.Application.Administration;
using Ekiphan.Domain.Content;
using Ekiphan.Domain.Media;
using Ekiphan.Domain.Quotes;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Ekiphan.Infrastructure.Administration;

internal sealed class AdminDashboardService(EkiphanDbContext dbContext)
    : IAdminDashboardService
{
    private const int ListLimit = 5;

    public async Task<AdminDashboard> GetAsync(
        AdminDashboardAccess access,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var databaseTimer = Stopwatch.StartNew();
        bool databaseReachable;
        try
        {
            databaseReachable = await dbContext.Database
                .CanConnectAsync(cancellationToken);
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            databaseReachable = false;
        }
        databaseTimer.Stop();

        var system = new AdminDashboardSystemStatus(
            true,
            databaseReachable,
            databaseTimer.ElapsedMilliseconds);
        if (!databaseReachable)
        {
            return new AdminDashboard(
                GeneratedAt: now,
                Products: null,
                TotalCategories: null,
                TotalBrands: null,
                Quotes: null,
                NewContactRequests: null,
                TotalMediaFiles: null,
                ContentUpdatesLast30Days: null,
                RecentQuotes: null,
                RecentProducts: null,
                ProductsMissingImages: null,
                ProductsMissingEnglishContent: null,
                LatestImport: null,
                System: system,
                Warnings: ["Veritabanı bağlantı kontrolü başarısız oldu."]);
        }

        AdminProductMetrics? productMetrics = null;
        int? totalCategories = null;
        int? totalBrands = null;
        IReadOnlyList<AdminDashboardProduct>? recentProducts = null;
        IReadOnlyList<AdminDashboardProduct>? productsMissingImages = null;
        IReadOnlyList<AdminDashboardProduct>? productsMissingEnglish = null;

        if (access.Catalog)
        {
            var products = dbContext.Products
                .AsNoTracking()
                .Where(item => !item.IsDeleted);
            var metrics = await products
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Total = group.Count(),
                    Published = group.Count(item => item.IsPublished),
                    Unpublished = group.Count(item => !item.IsPublished),
                })
                .SingleOrDefaultAsync(cancellationToken);

            var missingGalleryImage = await products.CountAsync(item =>
                !dbContext.ProductMedia.Any(media =>
                    media.ProductId == item.Id &&
                    media.Role == ProductMediaRole.GalleryImage),
                cancellationToken);
            var missingTurkishTranslation = await products.CountAsync(item =>
                !item.Translations.Any(text => text.LanguageCode == "tr"),
                cancellationToken);
            var missingEnglishTranslation = await products.CountAsync(item =>
                !item.Translations.Any(text => text.LanguageCode == "en"),
                cancellationToken);

            productMetrics = metrics is null
                ? new AdminProductMetrics(
                    0,
                    0,
                    0,
                    missingGalleryImage,
                    missingTurkishTranslation,
                    missingEnglishTranslation)
                : new AdminProductMetrics(
                    metrics.Total,
                    metrics.Published,
                    metrics.Unpublished,
                    missingGalleryImage,
                    missingTurkishTranslation,
                    missingEnglishTranslation);
            totalCategories = await dbContext.Categories
                .AsNoTracking()
                .CountAsync(cancellationToken);
            totalBrands = await dbContext.Brands
                .AsNoTracking()
                .CountAsync(cancellationToken);
            recentProducts = await ProjectProducts(
                    products
                        .OrderByDescending(item => item.UpdatedAt)
                        .ThenBy(item => item.SKU)
                        .Take(ListLimit))
                .ToListAsync(cancellationToken);
            productsMissingImages = await ProjectProducts(
                    products.Where(item =>
                        !dbContext.ProductMedia.Any(media =>
                            media.ProductId == item.Id &&
                            media.Role == ProductMediaRole.GalleryImage))
                        .OrderByDescending(item => item.UpdatedAt)
                        .ThenBy(item => item.SKU)
                        .Take(ListLimit))
                .ToListAsync(cancellationToken);
            productsMissingEnglish = await ProjectProducts(
                    products.Where(item =>
                        !item.Translations.Any(text =>
                            text.LanguageCode == "en"))
                        .OrderByDescending(item => item.UpdatedAt)
                        .ThenBy(item => item.SKU)
                        .Take(ListLimit))
                .ToListAsync(cancellationToken);
        }

        AdminQuoteMetrics? quoteMetrics = null;
        IReadOnlyList<AdminDashboardQuote>? recentQuotes = null;
        if (access.Quotes)
        {
            var quotes = dbContext.QuoteRequests.AsNoTracking();
            var metrics = await quotes
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    New = group.Count(item => item.Status == QuoteStatus.New),
                    InProgress = group.Count(item =>
                        item.Status == QuoteStatus.Reviewing ||
                        item.Status == QuoteStatus.Contacted ||
                        item.Status == QuoteStatus.Preparing),
                    Total = group.Count(),
                })
                .SingleOrDefaultAsync(cancellationToken);
            quoteMetrics = metrics is null
                ? new AdminQuoteMetrics(0, 0, 0)
                : new AdminQuoteMetrics(metrics.New, metrics.InProgress, metrics.Total);
            recentQuotes = await quotes
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new AdminDashboardQuote(
                    item.Id,
                    item.RequestNumber,
                    item.CompanyName,
                    item.Status.ToString(),
                    item.Items.Count,
                    item.CreatedAt))
                .Take(ListLimit)
                .ToListAsync(cancellationToken);
        }

        int? newContactRequests = null;
        if (access.Contacts)
        {
            newContactRequests = await dbContext.ContactRequests
                .AsNoTracking()
                .CountAsync(item => item.Status == ContactRequestStatus.New, cancellationToken);
        }

        int? totalMediaFiles = null;
        if (access.Media)
        {
            totalMediaFiles = await dbContext.MediaAssets
                .AsNoTracking()
                .CountAsync(cancellationToken);
        }

        int? contentUpdatesLast30Days = null;
        if (access.Content)
        {
            var since = now.AddDays(-30);
            var contentUpdateDates = dbContext.ContentPages.AsNoTracking()
                .Select(item => item.UpdatedAt)
                .Concat(dbContext.MenuItems.AsNoTracking()
                    .Select(item => item.UpdatedAt))
                .Concat(dbContext.HomepageHeroes.AsNoTracking()
                    .Select(item => item.UpdatedAt))
                .Concat(dbContext.GalleryItems.AsNoTracking()
                    .Select(item => item.UpdatedAt))
                .Concat(dbContext.PressReleases.AsNoTracking()
                    .Select(item => item.UpdatedAt));
            contentUpdatesLast30Days = await contentUpdateDates
                .CountAsync(updatedAt => updatedAt >= since, cancellationToken);
        }

        AdminLatestImport? latestImport = null;
        if (access.Imports)
        {
            latestImport = await dbContext.ImportJobs
                .AsNoTracking()
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new AdminLatestImport(
                    item.Id,
                    item.Status.ToString(),
                    item.SourceType.ToString(),
                    item.OriginalFileName,
                    item.TotalRowCount,
                    item.InvalidRowCount,
                    item.WarningCount,
                    item.CreatedAt))
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new AdminDashboard(
            GeneratedAt: now,
            Products: productMetrics,
            TotalCategories: totalCategories,
            TotalBrands: totalBrands,
            Quotes: quoteMetrics,
            NewContactRequests: newContactRequests,
            TotalMediaFiles: totalMediaFiles,
            ContentUpdatesLast30Days: contentUpdatesLast30Days,
            RecentQuotes: recentQuotes,
            RecentProducts: recentProducts,
            ProductsMissingImages: productsMissingImages,
            ProductsMissingEnglishContent: productsMissingEnglish,
            LatestImport: latestImport,
            System: system,
            Warnings: []);
    }

    private static IQueryable<AdminDashboardProduct> ProjectProducts(
        IQueryable<Ekiphan.Domain.Catalog.Product> products) =>
        products.Select(item => new AdminDashboardProduct(
            item.Id,
            item.SKU,
            item.Translations
                .Where(text => text.LanguageCode == "tr")
                .Select(text => text.Name)
                .FirstOrDefault() ??
            item.Translations
                .Where(text => text.LanguageCode == "en")
                .Select(text => text.Name)
                .FirstOrDefault() ??
            item.SKU,
            item.IsPublished,
            item.UpdatedAt));
}
