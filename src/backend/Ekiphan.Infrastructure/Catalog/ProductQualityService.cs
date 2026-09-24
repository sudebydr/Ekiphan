using Ekiphan.Application.Catalog;
using Ekiphan.Domain.Catalog;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Catalog;

public sealed class ProductQualityService(
    EkiphanDbContext dbContext,
    IEnumerable<IProductQualityRule> rules)
    : IProductQualityService
{
    public async Task<ProductQualityResultDto> EvaluateAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products.AsNoTracking()
            .Include(x => x.Categories)
            .Include(x => x.Translations)
            .Include(x => x.Tags)
            .Include(x => x.Variants)
            .SingleOrDefaultAsync(x => x.Id == productId && !x.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException(ProductManagementErrorCodes.ProductNotFound);

        var allActive = await dbContext.Products.AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var context = new ProductQualityContext(product, allActive);
        var issues = new List<ProductQualityIssueDto>();

        foreach (var rule in rules)
        {
            var ruleIssues = await rule.EvaluateAsync(context, cancellationToken);
            issues.AddRange(ruleIssues);
        }

        bool blocksPublishing = issues.Any(x => x.BlocksPublishing);

        int score = 100;
        foreach (var issue in issues)
        {
            int penalty = issue.Severity switch
            {
                ProductQualitySeverity.Critical => 30,
                ProductQualitySeverity.Error => 20,
                ProductQualitySeverity.Warning => 10,
                ProductQualitySeverity.Information => 2,
                _ => 0
            };
            score -= penalty;
        }

        score = Math.Clamp(score, 0, 100);
        var evaluatedAt = DateTimeOffset.UtcNow;

        return new ProductQualityResultDto(
            productId,
            score,
            blocksPublishing,
            evaluatedAt,
            issues);
    }

    public async Task<ProductQualitySummaryDto> EvaluateBatchAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ProductQualityResultDto>();
        foreach (var id in productIds)
        {
            try
            {
                var res = await EvaluateAsync(id, cancellationToken);
                results.Add(res);
            }
            catch (KeyNotFoundException)
            {
                // Skip missing
            }
        }

        int avg = results.Count == 0 ? 100 : (int)Math.Round(results.Average(r => r.QualityScore));
        int blocking = results.Count(r => r.BlocksPublishing);

        return new ProductQualitySummaryDto(
            results.Count,
            avg,
            blocking,
            results);
    }

    public async Task<ProductQualityDashboardDto> GetDashboardAsync(
        ProductQualityFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Products.AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => !x.IsDeleted);

        if (filter.WorkflowStatus.HasValue) query = query.Where(x => x.WorkflowStatus == filter.WorkflowStatus.Value);
        if (filter.BrandId.HasValue) query = query.Where(x => x.BrandId == filter.BrandId.Value);

        var products = await query.ToListAsync(cancellationToken);
        var allActive = products;

        var items = new List<ProductQualityDashboardItemDto>();
        int criticalCount = 0;
        int blockingCount = 0;
        int missingImagesCount = 0;
        int missingEnCount = 0;
        int missingSeoCount = 0;
        int duplicateSkuCount = 0;
        int duplicateSlugCount = 0;

        foreach (var p in products)
        {
            var ctx = new ProductQualityContext(p, allActive);
            var issues = new List<ProductQualityIssueDto>();
            foreach (var rule in rules)
            {
                issues.AddRange(await rule.EvaluateAsync(ctx, cancellationToken));
            }

            int score = 100;
            foreach (var issue in issues)
            {
                int penalty = issue.Severity switch
                {
                    ProductQualitySeverity.Critical => 30,
                    ProductQualitySeverity.Error => 20,
                    ProductQualitySeverity.Warning => 10,
                    ProductQualitySeverity.Information => 2,
                    _ => 0
                };
                score -= penalty;
            }
            score = Math.Clamp(score, 0, 100);
            bool blocks = issues.Any(x => x.BlocksPublishing);

            if (issues.Any(x => x.Severity == ProductQualitySeverity.Critical)) criticalCount++;
            if (blocks) blockingCount++;
            if (issues.Any(x => x.Code == "MISSING_PRIMARY_IMAGE")) missingImagesCount++;
            if (issues.Any(x => x.Code == "MISSING_EN_TRANSLATION")) missingEnCount++;
            if (issues.Any(x => x.Code == "MISSING_SEO_TITLE")) missingSeoCount++;
            if (issues.Any(x => x.Code == "DUPLICATE_SKU")) duplicateSkuCount++;
            if (issues.Any(x => x.Code == "DUPLICATE_SLUG")) duplicateSlugCount++;

            var trName = p.Translations.FirstOrDefault(t => t.LanguageCode == "tr")?.Name ?? p.SKU;
            items.Add(new ProductQualityDashboardItemDto(
                p.Id,
                p.SKU,
                trName,
                p.WorkflowStatus,
                score,
                blocks,
                issues.Count));
        }

        double avgScore = items.Count == 0 ? 100.0 : Math.Round(items.Average(i => i.QualityScore), 2);

        return new ProductQualityDashboardDto(
            items.Count,
            criticalCount,
            blockingCount,
            missingImagesCount,
            missingEnCount,
            missingSeoCount,
            duplicateSkuCount,
            duplicateSlugCount,
            avgScore,
            items.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList());
    }
}
