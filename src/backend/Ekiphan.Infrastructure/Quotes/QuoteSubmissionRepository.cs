using Ekiphan.Application.Quotes;
using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.Media;
using Ekiphan.Domain.Quotes;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Quotes;

internal sealed class QuoteSubmissionRepository(EkiphanDbContext dbContext)
    : IQuoteSubmissionRepository
{
    public async Task<IReadOnlyDictionary<Guid, QuoteProductSnapshot>>
        GetProductSnapshotsAsync(
            IReadOnlyCollection<Guid> productIds,
            string languageCode,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(productIds);
        var ids = productIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, QuoteProductSnapshot>();
        }

        var products = await dbContext.Products
            .AsNoTracking()
            .Where(product =>
                ids.Contains(product.Id) &&
                product.IsPublished &&
                product.Translations.Any(translation =>
                    translation.LanguageCode == languageCode))
            .Select(product => new
            {
                product.Id,
                product.SKU,
                Name = product.Translations
                    .Where(translation =>
                        translation.LanguageCode == languageCode)
                    .Select(translation => translation.Name)
                    .Single(),
                BrandName = dbContext.Brands
                    .Where(brand =>
                        brand.Id == product.BrandId &&
                        brand.IsPublished)
                    .Select(brand => brand.Name)
                    .SingleOrDefault(),
                ImageStorageKey = dbContext.ProductMedia
                    .Where(media =>
                        media.ProductId == product.Id &&
                        media.Role == ProductMediaRole.GalleryImage &&
                        media.IsDefault)
                    .Join(
                        dbContext.MediaAssets.Where(asset =>
                            asset.Status == MediaStatus.Active &&
                            asset.StorageKey != null),
                        media => media.MediaAssetId,
                        asset => asset.Id,
                        (_, asset) => asset.StorageKey)
                    .SingleOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var availableProductIds = products.Select(product => product.Id).ToArray();
        var variants = await dbContext.ProductVariants
            .AsNoTracking()
            .Where(variant =>
                availableProductIds.Contains(variant.ProductId) &&
                variant.IsActive)
            .Select(variant => new
            {
                variant.Id,
                variant.ProductId,
                variant.SKU,
            })
            .ToListAsync(cancellationToken);

        var variantIds = variants.Select(variant => variant.Id).ToArray();
        var selectionNames = await dbContext
            .Set<ProductVariantSelection>()
            .AsNoTracking()
            .Where(selection => variantIds.Contains(selection.ProductVariantId))
            .Join(
                dbContext.Set<ProductVariantGroup>(),
                selection => selection.VariantGroupId,
                group => group.Id,
                (selection, group) => new
                {
                    Selection = selection,
                    Group = group,
                })
            .Join(
                dbContext.Set<ProductVariantOption>(),
                item => item.Selection.VariantOptionId,
                option => option.Id,
                (item, option) => new
                {
                    item.Selection.ProductVariantId,
                    item.Group.SortOrder,
                    GroupName = item.Group.Translations
                        .Where(translation =>
                            translation.LanguageCode == languageCode)
                        .Select(translation => translation.Name)
                        .SingleOrDefault(),
                    OptionName = option.Translations
                        .Where(translation =>
                            translation.LanguageCode == languageCode)
                        .Select(translation => translation.Name)
                        .SingleOrDefault(),
                })
            .Where(item =>
                item.GroupName != null &&
                item.OptionName != null)
            .OrderBy(item => item.SortOrder)
            .ToListAsync(cancellationToken);

        var variantsByProduct = variants
            .GroupBy(variant => variant.ProductId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyDictionary<Guid, QuoteVariantSnapshot>)group
                    .ToDictionary(
                        variant => variant.Id,
                        variant => new QuoteVariantSnapshot(
                            variant.Id,
                            variant.SKU,
                            string.Join(
                                ", ",
                                selectionNames
                                    .Where(selection =>
                                        selection.ProductVariantId == variant.Id)
                                    .Select(selection =>
                                        $"{selection.GroupName}: {selection.OptionName}")))));

        return products.ToDictionary(
            product => product.Id,
            product => new QuoteProductSnapshot(
                product.Id,
                product.Name,
                product.SKU,
                product.BrandName,
                product.ImageStorageKey,
                variantsByProduct.GetValueOrDefault(product.Id) ??
                    new Dictionary<Guid, QuoteVariantSnapshot>()));
    }

    public void Add(QuoteRequest quoteRequest)
    {
        ArgumentNullException.ThrowIfNull(quoteRequest);
        dbContext.QuoteRequests.Add(quoteRequest);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
