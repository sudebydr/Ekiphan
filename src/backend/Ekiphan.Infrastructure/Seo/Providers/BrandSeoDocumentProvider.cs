using Ekiphan.Application.Seo;
using Ekiphan.Domain.Seo;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Seo.Providers;

public sealed class BrandSeoDocumentProvider : ISeoDocumentProvider
{
    private readonly EkiphanDbContext _dbContext;

    public BrandSeoDocumentProvider(EkiphanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public SeoEntityType EntityType => SeoEntityType.Brand;

    public async Task<SeoDocument?> GetAsync(Guid entityId, string languageCode, CancellationToken cancellationToken)
    {
        var brand = await _dbContext.Brands
            .AsNoTracking()
            .Include(b => b.Translations)
            .FirstOrDefaultAsync(b => b.Id == entityId, cancellationToken);

        if (brand == null) return null;

        var translation = brand.Translations.FirstOrDefault(t => t.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
                       ?? brand.Translations.FirstOrDefault();

        if (translation == null) return null;

        var lang = translation.LanguageCode.ToLowerInvariant();
        return new SeoDocument
        {
            EntityType = SeoEntityType.Brand,
            EntityId = brand.Id,
            LanguageCode = lang,
            Slug = translation.Slug,
            Url = $"/{lang}/markalar/{translation.Slug}",
            MetaTitle = brand.Name,
            MetaDescription = translation.Description,
            CanonicalUrl = null,
            OpenGraphTitle = brand.Name,
            OpenGraphDescription = translation.Description,
            OpenGraphMediaAssetId = null,
            NoIndex = false,
            NoFollow = false,
            PublishedAt = brand.CreatedAt,
            UpdatedAt = brand.UpdatedAt,
            IsPublished = brand.IsPublished,
            IsArchived = false
        };
    }

    public async IAsyncEnumerable<SeoDocument> StreamAllAsync(string languageCode, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var query = _dbContext.Brands
            .AsNoTracking()
            .Include(b => b.Translations);

        await foreach (var brand in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var translation = brand.Translations.FirstOrDefault(t => t.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
                           ?? brand.Translations.FirstOrDefault();

            if (translation != null)
            {
                var lang = translation.LanguageCode.ToLowerInvariant();
                yield return new SeoDocument
                {
                    EntityType = SeoEntityType.Brand,
                    EntityId = brand.Id,
                    LanguageCode = lang,
                    Slug = translation.Slug,
                    Url = $"/{lang}/markalar/{translation.Slug}",
                    MetaTitle = brand.Name,
                    MetaDescription = translation.Description,
                    CanonicalUrl = null,
                    OpenGraphTitle = brand.Name,
                    OpenGraphDescription = translation.Description,
                    OpenGraphMediaAssetId = null,
                    NoIndex = false,
                    NoFollow = false,
                    PublishedAt = brand.CreatedAt,
                    UpdatedAt = brand.UpdatedAt,
                    IsPublished = brand.IsPublished,
                    IsArchived = false
                };
            }
        }
    }
}
