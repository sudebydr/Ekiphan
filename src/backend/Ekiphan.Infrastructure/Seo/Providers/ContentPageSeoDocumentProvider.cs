using Ekiphan.Application.Seo;
using Ekiphan.Domain.Content;
using Ekiphan.Domain.Seo;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Seo.Providers;

public sealed class ContentPageSeoDocumentProvider : ISeoDocumentProvider
{
    private readonly EkiphanDbContext _dbContext;

    public ContentPageSeoDocumentProvider(EkiphanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public SeoEntityType EntityType => SeoEntityType.ContentPage;

    public async Task<SeoDocument?> GetAsync(Guid entityId, string languageCode, CancellationToken cancellationToken)
    {
        var page = await _dbContext.ContentPages
            .AsNoTracking()
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == entityId, cancellationToken);

        if (page == null) return null;

        var translation = page.Translations.FirstOrDefault(t => t.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
                       ?? page.Translations.FirstOrDefault();

        if (translation == null) return null;

        var lang = translation.LanguageCode.ToLowerInvariant();
        return new SeoDocument
        {
            EntityType = SeoEntityType.ContentPage,
            EntityId = page.Id,
            LanguageCode = lang,
            Slug = translation.Slug,
            Url = $"/{lang}/kurumsal/{translation.Slug}",
            MetaTitle = translation.MetaTitle,
            MetaDescription = translation.MetaDescription,
            CanonicalUrl = translation.CanonicalUrl,
            OpenGraphTitle = translation.OpenGraphTitle,
            OpenGraphDescription = translation.OpenGraphDescription,
            OpenGraphMediaAssetId = translation.OpenGraphImageMediaId,
            NoIndex = translation.NoIndex,
            NoFollow = translation.NoFollow,
            PublishedAt = page.PublishedAt,
            UpdatedAt = page.UpdatedAt,
            IsPublished = page.Status == ContentStatus.Published,
            IsArchived = false
        };
    }

    public async IAsyncEnumerable<SeoDocument> StreamAllAsync(string languageCode, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var query = _dbContext.ContentPages
            .AsNoTracking()
            .Include(p => p.Translations);

        await foreach (var page in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var translation = page.Translations.FirstOrDefault(t => t.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
                           ?? page.Translations.FirstOrDefault();

            if (translation != null)
            {
                var lang = translation.LanguageCode.ToLowerInvariant();
                yield return new SeoDocument
                {
                    EntityType = SeoEntityType.ContentPage,
                    EntityId = page.Id,
                    LanguageCode = lang,
                    Slug = translation.Slug,
                    Url = $"/{lang}/kurumsal/{translation.Slug}",
                    MetaTitle = translation.MetaTitle,
                    MetaDescription = translation.MetaDescription,
                    CanonicalUrl = translation.CanonicalUrl,
                    OpenGraphTitle = translation.OpenGraphTitle,
                    OpenGraphDescription = translation.OpenGraphDescription,
                    OpenGraphMediaAssetId = translation.OpenGraphImageMediaId,
                    NoIndex = translation.NoIndex,
                    NoFollow = translation.NoFollow,
                    PublishedAt = page.PublishedAt,
                    UpdatedAt = page.UpdatedAt,
                    IsPublished = page.Status == ContentStatus.Published,
                    IsArchived = false
                };
            }
        }
    }
}
