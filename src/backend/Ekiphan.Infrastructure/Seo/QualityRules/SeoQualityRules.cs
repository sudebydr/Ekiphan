using Ekiphan.Application.Seo;
using Ekiphan.Domain.Seo;

namespace Ekiphan.Infrastructure.Seo.QualityRules;

public sealed class MissingMetaTitleRule : ISeoQualityRule
{
    public string Code => "SEO_MISSING_META_TITLE";
    public SeoEntityType? EntityType => null;

    public Task<IReadOnlyCollection<SeoQualityIssueDto>> EvaluateAsync(SeoQualityContext context, CancellationToken cancellationToken)
    {
        var issues = new List<SeoQualityIssueDto>();
        var doc = context.Document;

        if (string.IsNullOrWhiteSpace(doc.MetaTitle))
        {
            issues.Add(new SeoQualityIssueDto(
                Code, QualityIssueSeverity.Error, doc.EntityType, doc.EntityId, doc.LanguageCode,
                "MetaTitle", "Meta title is missing.", "Provide a descriptive meta title (30-60 characters).", false, DateTimeOffset.UtcNow));
        }
        else if (doc.MetaTitle.Length < 30)
        {
            issues.Add(new SeoQualityIssueDto(
                "SEO_SHORT_META_TITLE", QualityIssueSeverity.Warning, doc.EntityType, doc.EntityId, doc.LanguageCode,
                "MetaTitle", "Meta title is too short.", "Expand title length to at least 30 characters.", false, DateTimeOffset.UtcNow));
        }
        else if (doc.MetaTitle.Length > 70)
        {
            issues.Add(new SeoQualityIssueDto(
                "SEO_LONG_META_TITLE", QualityIssueSeverity.Warning, doc.EntityType, doc.EntityId, doc.LanguageCode,
                "MetaTitle", "Meta title is too long.", "Reduce title length below 70 characters.", false, DateTimeOffset.UtcNow));
        }

        return Task.FromResult<IReadOnlyCollection<SeoQualityIssueDto>>(issues);
    }
}

public sealed class MissingMetaDescriptionRule : ISeoQualityRule
{
    public string Code => "SEO_MISSING_META_DESCRIPTION";
    public SeoEntityType? EntityType => null;

    public Task<IReadOnlyCollection<SeoQualityIssueDto>> EvaluateAsync(SeoQualityContext context, CancellationToken cancellationToken)
    {
        var issues = new List<SeoQualityIssueDto>();
        var doc = context.Document;

        if (string.IsNullOrWhiteSpace(doc.MetaDescription))
        {
            issues.Add(new SeoQualityIssueDto(
                Code, QualityIssueSeverity.Warning, doc.EntityType, doc.EntityId, doc.LanguageCode,
                "MetaDescription", "Meta description is missing.", "Provide a meta description (70-160 characters).", false, DateTimeOffset.UtcNow));
        }

        return Task.FromResult<IReadOnlyCollection<SeoQualityIssueDto>>(issues);
    }
}

public sealed class DuplicateSlugRule : ISeoQualityRule
{
    public string Code => "SEO_DUPLICATE_SLUG";
    public SeoEntityType? EntityType => null;

    public Task<IReadOnlyCollection<SeoQualityIssueDto>> EvaluateAsync(SeoQualityContext context, CancellationToken cancellationToken)
    {
        var issues = new List<SeoQualityIssueDto>();
        var doc = context.Document;

        var duplicate = context.AllDocumentsInLanguage.Any(d =>
            d.EntityId != doc.EntityId &&
            d.EntityType == doc.EntityType &&
            string.Equals(d.Slug, doc.Slug, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            issues.Add(new SeoQualityIssueDto(
                Code, QualityIssueSeverity.Critical, doc.EntityType, doc.EntityId, doc.LanguageCode,
                "Slug", "Duplicate slug detected for this entity type in the same language.", "Update slug to be unique.", true, DateTimeOffset.UtcNow));
        }

        return Task.FromResult<IReadOnlyCollection<SeoQualityIssueDto>>(issues);
    }
}
