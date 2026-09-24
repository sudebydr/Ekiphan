using Ekiphan.Domain.Seo;

namespace Ekiphan.Application.Seo;

public enum QualityIssueSeverity
{
    Information = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}

public sealed record SeoQualityIssueDto(
    string Code,
    QualityIssueSeverity Severity,
    SeoEntityType EntityType,
    Guid EntityId,
    string LanguageCode,
    string Field,
    string Message,
    string SuggestedAction,
    bool BlocksPublishing,
    DateTimeOffset DetectedAt);

public sealed record SeoQualityContext(
    SeoDocument Document,
    IReadOnlyList<SeoDocument> AllDocumentsInLanguage);

public sealed record SeoQualityResultDto(
    SeoEntityType EntityType,
    Guid EntityId,
    string LanguageCode,
    int Score,
    IReadOnlyList<SeoQualityIssueDto> Issues,
    DateTimeOffset CalculatedAt);

public sealed record SeoQualityDashboardDto(
    int TotalEntities,
    int CriticalIssueCount,
    int ErrorCount,
    int WarningCount,
    int MissingMetaTitleCount,
    int MissingMetaDescriptionCount,
    int DuplicateTitleCount,
    int DuplicateSlugCount,
    int MissingCanonicalCount,
    int MissingAltTextCount,
    int BrokenLinkCount,
    int RedirectLoopCount,
    int SitemapMismatchCount,
    double AverageSeoScore);

public sealed record SeoQualityFilter(
    SeoEntityType? EntityType = null,
    string? LanguageCode = null,
    QualityIssueSeverity? Severity = null,
    string? IssueCode = null,
    bool? BlocksPublishing = null,
    int? MinScore = null,
    int? MaxScore = null,
    int Page = 1,
    int PageSize = 25);

public interface ISeoQualityRule
{
    string Code { get; }
    SeoEntityType? EntityType { get; }
    Task<IReadOnlyCollection<SeoQualityIssueDto>> EvaluateAsync(SeoQualityContext context, CancellationToken cancellationToken);
}

public interface ISeoQualityService
{
    Task<SeoQualityDashboardDto> GetDashboardAsync(CancellationToken cancellationToken);
    Task<SeoQualityResultDto?> GetEntityQualityAsync(SeoEntityType entityType, Guid entityId, string languageCode, CancellationToken cancellationToken);
    Task RecalculateAllAsync(CancellationToken cancellationToken);
    Task<SeoQualityResultDto> RecalculateEntityAsync(SeoEntityType entityType, Guid entityId, string languageCode, CancellationToken cancellationToken);
}
