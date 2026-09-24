using Ekiphan.Application.Seo;
using Ekiphan.Domain.Seo;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Seo.Providers;

public sealed class ProductSeoDocumentProvider : ISeoDocumentProvider
{
    private readonly EkiphanDbContext _dbContext;

    public ProductSeoDocumentProvider(EkiphanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public SeoEntityType EntityType => SeoEntityType.Product;

    public async Task<SeoDocument?> GetAsync(Guid entityId, string languageCode, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == entityId && !p.IsDeleted, cancellationToken);

        if (product == null) return null;

        var translation = product.Translations.FirstOrDefault(t => t.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
                       ?? product.Translations.FirstOrDefault();

        if (translation == null) return null;

        return MapToDocument(product, translation);
    }

    public async IAsyncEnumerable<SeoDocument> StreamAllAsync(string languageCode, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var query = _dbContext.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Include(p => p.Translations);

        await foreach (var product in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var translation = product.Translations.FirstOrDefault(t => t.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
                           ?? product.Translations.FirstOrDefault();

            if (translation != null)
            {
                yield return MapToDocument(product, translation);
            }
        }
    }

    private static SeoDocument MapToDocument(Domain.Catalog.Product product, Domain.Catalog.ProductTranslation translation)
    {
        var lang = translation.LanguageCode.ToLowerInvariant();
        return new SeoDocument
        {
            EntityType = SeoEntityType.Product,
            EntityId = product.Id,
            LanguageCode = lang,
            Slug = translation.Slug,
            Url = $"/{lang}/urunler/{translation.Slug}",
            MetaTitle = translation.MetaTitle,
            MetaDescription = translation.MetaDescription,
            CanonicalUrl = translation.CanonicalUrl,
            OpenGraphTitle = translation.OpenGraphTitle,
            OpenGraphDescription = translation.OpenGraphDescription,
            OpenGraphMediaAssetId = translation.OpenGraphImageMediaId,
            NoIndex = translation.NoIndex,
            NoFollow = translation.NoFollow,
            PublishedAt = product.PublishedAt,
            UpdatedAt = product.UpdatedAt,
            IsPublished = product.IsPublished,
            IsArchived = product.WorkflowStatus == Domain.Catalog.ProductWorkflowStatus.Archived
        };
    }
}
