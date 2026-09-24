using System.Text.Json;
using Ekiphan.Application.Seo;
using Ekiphan.Domain.Seo;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Seo;

public sealed class SeoQualityService : ISeoQualityService
{
    private readonly IEnumerable<ISeoQualityRule> _rules;
    private readonly IEnumerable<ISeoDocumentProvider> _documentProviders;
    private readonly EkiphanDbContext _dbContext;

    public SeoQualityService(
        IEnumerable<ISeoQualityRule> rules,
        IEnumerable<ISeoDocumentProvider> documentProviders,
        EkiphanDbContext dbContext)
    {
        _rules = rules;
        _documentProviders = documentProviders;
        _dbContext = dbContext;
    }

    public async Task<SeoQualityDashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var snapshots = await _dbContext.SeoQualitySnapshots.AsNoTracking().ToListAsync(cancellationToken);

        int total = snapshots.Count;
        int critical = snapshots.Sum(s => s.CriticalCount);
        int errors = snapshots.Sum(s => s.ErrorCount);
        int warnings = snapshots.Sum(s => s.WarningCount);
        double avgScore = total > 0 ? snapshots.Average(s => s.Score) : 100.0;

        return new SeoQualityDashboardDto(
            TotalEntities: total,
            CriticalIssueCount: critical,
            ErrorCount: errors,
            WarningCount: warnings,
            MissingMetaTitleCount: errors,
            MissingMetaDescriptionCount: warnings,
            DuplicateTitleCount: 0,
            DuplicateSlugCount: critical,
            MissingCanonicalCount: 0,
            MissingAltTextCount: 0,
            BrokenLinkCount: 0,
            RedirectLoopCount: 0,
            SitemapMismatchCount: 0,
            AverageSeoScore: avgScore);
    }

    public async Task<SeoQualityResultDto?> GetEntityQualityAsync(SeoEntityType entityType, Guid entityId, string languageCode, CancellationToken cancellationToken)
    {
        var snapshot = await _dbContext.SeoQualitySnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.EntityType == entityType && s.EntityId == entityId && s.LanguageCode == languageCode, cancellationToken);

        if (snapshot == null)
        {
            return await RecalculateEntityAsync(entityType, entityId, languageCode, cancellationToken);
        }

        var issues = JsonSerializer.Deserialize<List<SeoQualityIssueDto>>(snapshot.IssuesJson) ?? new List<SeoQualityIssueDto>();
        return new SeoQualityResultDto(entityType, entityId, languageCode, snapshot.Score, issues, snapshot.CalculatedAt);
    }

    public async Task RecalculateAllAsync(CancellationToken cancellationToken)
    {
        foreach (var provider in _documentProviders)
        {
            await foreach (var doc in provider.StreamAllAsync("tr", cancellationToken))
            {
                await RecalculateEntityAsync(doc.EntityType, doc.EntityId, doc.LanguageCode, cancellationToken);
            }
        }
    }

    public async Task<SeoQualityResultDto> RecalculateEntityAsync(SeoEntityType entityType, Guid entityId, string languageCode, CancellationToken cancellationToken)
    {
        var provider = _documentProviders.FirstOrDefault(p => p.EntityType == entityType);
        if (provider == null)
        {
            return new SeoQualityResultDto(entityType, entityId, languageCode, 100, Array.Empty<SeoQualityIssueDto>(), DateTimeOffset.UtcNow);
        }

        var doc = await provider.GetAsync(entityId, languageCode, cancellationToken);
        if (doc == null)
        {
            return new SeoQualityResultDto(entityType, entityId, languageCode, 100, Array.Empty<SeoQualityIssueDto>(), DateTimeOffset.UtcNow);
        }

        var allDocs = new List<SeoDocument>();
        await foreach (var d in provider.StreamAllAsync(languageCode, cancellationToken))
        {
            allDocs.Add(d);
        }

        var context = new SeoQualityContext(doc, allDocs);
        var issues = new List<SeoQualityIssueDto>();

        foreach (var rule in _rules)
        {
            if (rule.EntityType == null || rule.EntityType == entityType)
            {
                var ruleIssues = await rule.EvaluateAsync(context, cancellationToken);
                issues.AddRange(ruleIssues);
            }
        }

        int criticalCount = issues.Count(i => i.Severity == QualityIssueSeverity.Critical);
        int errorCount = issues.Count(i => i.Severity == QualityIssueSeverity.Error);
        int warningCount = issues.Count(i => i.Severity == QualityIssueSeverity.Warning);

        int penalty = (criticalCount * 25) + (errorCount * 10) + (warningCount * 3);
        int score = Math.Clamp(100 - penalty, 0, 100);

        string issuesJson = JsonSerializer.Serialize(issues);

        var existing = await _dbContext.SeoQualitySnapshots
            .FirstOrDefaultAsync(s => s.EntityType == entityType && s.EntityId == entityId && s.LanguageCode == languageCode, cancellationToken);

        if (existing == null)
        {
            existing = new SeoQualitySnapshot(Guid.NewGuid(), entityType, entityId, languageCode, score, criticalCount, errorCount, warningCount, issuesJson, 1);
            _dbContext.SeoQualitySnapshots.Add(existing);
        }
        else
        {
            existing.Update(score, criticalCount, errorCount, warningCount, issuesJson);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new SeoQualityResultDto(entityType, entityId, languageCode, score, issues, DateTimeOffset.UtcNow);
    }
}
