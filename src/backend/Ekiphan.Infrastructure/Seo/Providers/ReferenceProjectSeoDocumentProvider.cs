using Ekiphan.Application.Seo;
using Ekiphan.Domain.Content;
using Ekiphan.Domain.Seo;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Seo.Providers;

public sealed class ReferenceProjectSeoDocumentProvider : ISeoDocumentProvider
{
    private readonly EkiphanDbContext _dbContext;

    public ReferenceProjectSeoDocumentProvider(EkiphanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public SeoEntityType EntityType => SeoEntityType.ReferenceProject;

    public async Task<SeoDocument?> GetAsync(Guid entityId, string languageCode, CancellationToken cancellationToken)
    {
        var refProj = await _dbContext.ReferenceProjects
            .AsNoTracking()
            .Include(r => r.Translations)
            .FirstOrDefaultAsync(r => r.Id == entityId, cancellationToken);

        if (refProj == null) return null;

        var translation = refProj.Translations.FirstOrDefault(t => t.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
                       ?? refProj.Translations.FirstOrDefault();

        if (translation == null) return null;

        var lang = translation.LanguageCode.ToLowerInvariant();
        return new SeoDocument
        {
            EntityType = SeoEntityType.ReferenceProject,
            EntityId = refProj.Id,
            LanguageCode = lang,
            Slug = translation.Slug,
            Url = $"/{lang}/referanslar/{translation.Slug}",
            MetaTitle = translation.MetaTitle ?? translation.Title,
            MetaDescription = translation.MetaDescription ?? translation.ShortDescription,
            CanonicalUrl = translation.CanonicalUrl,
            OpenGraphTitle = translation.OpenGraphTitle ?? translation.Title,
            OpenGraphDescription = translation.OpenGraphDescription ?? translation.ShortDescription,
            OpenGraphMediaAssetId = translation.OpenGraphMediaAssetId,
            NoIndex = false,
            NoFollow = false,
            PublishedAt = refProj.PublishedAt ?? refProj.CreatedAt,
            UpdatedAt = refProj.UpdatedAt,
            IsPublished = refProj.WorkflowStatus == ContentWorkflowStatus.Published,
            IsArchived = refProj.WorkflowStatus == ContentWorkflowStatus.Archived
        };
    }

    public async IAsyncEnumerable<SeoDocument> StreamAllAsync(string languageCode, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var query = _dbContext.ReferenceProjects
            .AsNoTracking()
            .Include(r => r.Translations);

        await foreach (var refProj in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var translation = refProj.Translations.FirstOrDefault(t => t.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
                           ?? refProj.Translations.FirstOrDefault();

            if (translation != null)
            {
                var lang = translation.LanguageCode.ToLowerInvariant();
                yield return new SeoDocument
                {
                    EntityType = SeoEntityType.ReferenceProject,
                    EntityId = refProj.Id,
                    LanguageCode = lang,
                    Slug = translation.Slug,
                    Url = $"/{lang}/referanslar/{translation.Slug}",
                    MetaTitle = translation.MetaTitle ?? translation.Title,
                    MetaDescription = translation.MetaDescription ?? translation.ShortDescription,
                    CanonicalUrl = translation.CanonicalUrl,
                    OpenGraphTitle = translation.OpenGraphTitle ?? translation.Title,
                    OpenGraphDescription = translation.OpenGraphDescription ?? translation.ShortDescription,
                    OpenGraphMediaAssetId = translation.OpenGraphMediaAssetId,
                    NoIndex = false,
                    NoFollow = false,
                    PublishedAt = refProj.PublishedAt ?? refProj.CreatedAt,
                    UpdatedAt = refProj.UpdatedAt,
                    IsPublished = refProj.WorkflowStatus == ContentWorkflowStatus.Published,
                    IsArchived = refProj.WorkflowStatus == ContentWorkflowStatus.Archived
                };
            }
        }
    }
}
