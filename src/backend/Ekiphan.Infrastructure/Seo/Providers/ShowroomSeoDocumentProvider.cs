using Ekiphan.Application.Seo;
using Ekiphan.Domain.Content;
using Ekiphan.Domain.Seo;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Seo.Providers;

public sealed class ShowroomSeoDocumentProvider : ISeoDocumentProvider
{
    private readonly EkiphanDbContext _dbContext;

    public ShowroomSeoDocumentProvider(EkiphanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public SeoEntityType EntityType => SeoEntityType.Showroom;

    public async Task<SeoDocument?> GetAsync(Guid entityId, string languageCode, CancellationToken cancellationToken)
    {
        var showroom = await _dbContext.Showrooms
            .AsNoTracking()
            .Include(s => s.Translations)
            .FirstOrDefaultAsync(s => s.Id == entityId, cancellationToken);

        if (showroom == null) return null;

        var translation = showroom.Translations.FirstOrDefault(t => t.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
                       ?? showroom.Translations.FirstOrDefault();

        if (translation == null) return null;

        var lang = translation.LanguageCode.ToLowerInvariant();
        return new SeoDocument
        {
            EntityType = SeoEntityType.Showroom,
            EntityId = showroom.Id,
            LanguageCode = lang,
            Slug = translation.Slug,
            Url = $"/{lang}/showroom/{translation.Slug}",
            MetaTitle = translation.MetaTitle ?? translation.Title,
            MetaDescription = translation.MetaDescription ?? translation.ShortDescription,
            CanonicalUrl = null,
            OpenGraphTitle = translation.OpenGraphTitle ?? translation.Title,
            OpenGraphDescription = translation.OpenGraphDescription ?? translation.ShortDescription,
            OpenGraphMediaAssetId = translation.OpenGraphMediaAssetId,
            NoIndex = false,
            NoFollow = false,
            PublishedAt = showroom.PublishedAt ?? showroom.CreatedAt,
            UpdatedAt = showroom.UpdatedAt,
            IsPublished = showroom.WorkflowStatus == ContentWorkflowStatus.Published,
            IsArchived = showroom.WorkflowStatus == ContentWorkflowStatus.Archived
        };
    }

    public async IAsyncEnumerable<SeoDocument> StreamAllAsync(string languageCode, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var query = _dbContext.Showrooms
            .AsNoTracking()
            .Include(s => s.Translations);

        await foreach (var showroom in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var translation = showroom.Translations.FirstOrDefault(t => t.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
                           ?? showroom.Translations.FirstOrDefault();

            if (translation != null)
            {
                var lang = translation.LanguageCode.ToLowerInvariant();
                yield return new SeoDocument
                {
                    EntityType = SeoEntityType.Showroom,
                    EntityId = showroom.Id,
                    LanguageCode = lang,
                    Slug = translation.Slug,
                    Url = $"/{lang}/showroom/{translation.Slug}",
                    MetaTitle = translation.MetaTitle ?? translation.Title,
                    MetaDescription = translation.MetaDescription ?? translation.ShortDescription,
                    CanonicalUrl = null,
                    OpenGraphTitle = translation.OpenGraphTitle ?? translation.Title,
                    OpenGraphDescription = translation.OpenGraphDescription ?? translation.ShortDescription,
                    OpenGraphMediaAssetId = translation.OpenGraphMediaAssetId,
                    NoIndex = false,
                    NoFollow = false,
                    PublishedAt = showroom.PublishedAt ?? showroom.CreatedAt,
                    UpdatedAt = showroom.UpdatedAt,
                    IsPublished = showroom.WorkflowStatus == ContentWorkflowStatus.Published,
                    IsArchived = showroom.WorkflowStatus == ContentWorkflowStatus.Archived
                };
            }
        }
    }
}
