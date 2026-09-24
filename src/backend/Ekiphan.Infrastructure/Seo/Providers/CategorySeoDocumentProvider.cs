using Ekiphan.Application.Seo;
using Ekiphan.Domain.Seo;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Seo.Providers;

public sealed class CategorySeoDocumentProvider : ISeoDocumentProvider
{
    private readonly EkiphanDbContext _dbContext;

    public CategorySeoDocumentProvider(EkiphanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public SeoEntityType EntityType => SeoEntityType.Category;

    public async Task<SeoDocument?> GetAsync(Guid entityId, string languageCode, CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories
            .AsNoTracking()
            .Include(c => c.Translations)
            .FirstOrDefaultAsync(c => c.Id == entityId, cancellationToken);

        if (category == null) return null;

        var translation = category.Translations.FirstOrDefault(t => t.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
                       ?? category.Translations.FirstOrDefault();

        if (translation == null) return null;

        return MapToDocument(category, translation);
    }

    public async IAsyncEnumerable<SeoDocument> StreamAllAsync(string languageCode, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var query = _dbContext.Categories
            .AsNoTracking()
            .Include(c => c.Translations);

        await foreach (var category in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var translation = category.Translations.FirstOrDefault(t => t.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
                           ?? category.Translations.FirstOrDefault();

            if (translation != null)
            {
                yield return MapToDocument(category, translation);
            }
        }
    }

    private static SeoDocument MapToDocument(Domain.Catalog.Category category, Domain.Catalog.CategoryTranslation translation)
    {
        var lang = translation.LanguageCode.ToLowerInvariant();
        return new SeoDocument
        {
            EntityType = SeoEntityType.Category,
            EntityId = category.Id,
            LanguageCode = lang,
            Slug = translation.Slug,
            Url = $"/{lang}/kategoriler/{translation.Slug}",
            MetaTitle = translation.MetaTitle,
            MetaDescription = translation.MetaDescription,
            CanonicalUrl = translation.CanonicalUrl,
            OpenGraphTitle = translation.OpenGraphTitle,
            OpenGraphDescription = translation.OpenGraphDescription,
            OpenGraphMediaAssetId = translation.OpenGraphImageMediaId,
            NoIndex = translation.NoIndex,
            NoFollow = translation.NoFollow,
            PublishedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt,
            IsPublished = category.IsPublished,
            IsArchived = false
        };
    }
}
