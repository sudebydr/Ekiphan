using Ekiphan.Application.Seo;
using Ekiphan.Domain.Seo;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Seo.Providers;

public sealed class NewsSeoDocumentProvider : ISeoDocumentProvider
{
    private readonly EkiphanDbContext _dbContext;

    public NewsSeoDocumentProvider(EkiphanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public SeoEntityType EntityType => SeoEntityType.NewsArticle;

    public async Task<SeoDocument?> GetAsync(Guid entityId, string languageCode, CancellationToken cancellationToken)
    {
        var press = await _dbContext.PressReleases
            .AsNoTracking()
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == entityId, cancellationToken);

        if (press == null) return null;

        var translation = press.Translations.FirstOrDefault(t => t.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
                       ?? press.Translations.FirstOrDefault();

        if (translation == null) return null;

        var lang = translation.LanguageCode.ToLowerInvariant();
        return new SeoDocument
        {
            EntityType = SeoEntityType.NewsArticle,
            EntityId = press.Id,
            LanguageCode = lang,
            Slug = translation.Slug,
            Url = $"/{lang}/basin/{translation.Slug}",
            MetaTitle = translation.MetaTitle,
            MetaDescription = translation.MetaDescription,
            CanonicalUrl = translation.CanonicalUrl,
            OpenGraphTitle = translation.OpenGraphTitle,
            OpenGraphDescription = translation.OpenGraphDescription,
            OpenGraphMediaAssetId = translation.OpenGraphImageMediaId,
            NoIndex = translation.NoIndex,
            NoFollow = translation.NoFollow,
            PublishedAt = press.PublishedAt,
            UpdatedAt = press.UpdatedAt,
            IsPublished = press.IsPublished,
            IsArchived = false
        };
    }

    public async IAsyncEnumerable<SeoDocument> StreamAllAsync(string languageCode, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var query = _dbContext.PressReleases
            .AsNoTracking()
            .Include(p => p.Translations);

        await foreach (var press in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var translation = press.Translations.FirstOrDefault(t => t.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
                           ?? press.Translations.FirstOrDefault();

            if (translation != null)
            {
                var lang = translation.LanguageCode.ToLowerInvariant();
                yield return new SeoDocument
                {
                    EntityType = SeoEntityType.NewsArticle,
                    EntityId = press.Id,
                    LanguageCode = lang,
                    Slug = translation.Slug,
                    Url = $"/{lang}/basin/{translation.Slug}",
                    MetaTitle = translation.MetaTitle,
                    MetaDescription = translation.MetaDescription,
                    CanonicalUrl = translation.CanonicalUrl,
                    OpenGraphTitle = translation.OpenGraphTitle,
                    OpenGraphDescription = translation.OpenGraphDescription,
                    OpenGraphMediaAssetId = translation.OpenGraphImageMediaId,
                    NoIndex = translation.NoIndex,
                    NoFollow = translation.NoFollow,
                    PublishedAt = press.PublishedAt,
                    UpdatedAt = press.UpdatedAt,
                    IsPublished = press.IsPublished,
                    IsArchived = false
                };
            }
        }
    }
}
