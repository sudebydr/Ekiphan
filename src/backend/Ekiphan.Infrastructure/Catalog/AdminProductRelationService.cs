using Ekiphan.Application.Catalog;
using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.Media;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Catalog;

internal sealed class AdminProductRelationService(EkiphanDbContext dbContext)
    : IAdminProductRelationService
{
    public async Task<AdminCatalogProductPage> GetProductsAsync(
        AdminProductListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ValidatePagination(query.Page, query.PageSize);
        var language = ValidateLanguage(query.LanguageCode);
        if (!Enum.IsDefined(query.SortBy) ||
            !Enum.IsDefined(query.SortDirection))
        {
            throw new ArgumentOutOfRangeException(nameof(query));
        }

        var products = FilterProducts(
            language,
            query.Search,
            query.CategoryId,
            query.BrandId,
            query.IsPublished,
            query.MissingGalleryImage,
            query.MissingEnglishContent);

        var totalCount = await products.CountAsync(cancellationToken);
        var ordered = OrderProducts(
            products,
            language,
            query.SortBy,
            query.SortDirection);
        var items = await ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(product => new AdminCatalogProductSummary(
                product.Id,
                product.SKU,
                product.Translations
                    .Where(translation =>
                        translation.LanguageCode == language)
                    .Select(translation => translation.Name)
                    .Single(),
                product.IsPublished,
                product.BrandId,
                dbContext.Brands
                    .Where(brand => brand.Id == product.BrandId)
                    .Select(brand => brand.Name)
                    .FirstOrDefault(),
                product.PrimaryCategoryId,
                dbContext.Categories
                    .Where(category => category.Id == product.PrimaryCategoryId)
                    .SelectMany(category => category.Translations)
                    .Where(translation => translation.LanguageCode == language)
                    .Select(translation => translation.Name)
                    .FirstOrDefault(),
                !dbContext.ProductMedia.Any(media =>
                    media.ProductId == product.Id &&
                    media.Role == ProductMediaRole.GalleryImage),
                !product.Translations.Any(translation =>
                    translation.LanguageCode == "en"),
                product.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new AdminCatalogProductPage(
            items,
            query.Page,
            query.PageSize,
            totalCount);
    }

    public async Task<IReadOnlyList<Guid>> SelectUnpublishedProductIdsAsync(
        AdminProductBulkSelectionQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var productIds = await FilterProducts(
                ValidateLanguage(query.LanguageCode),
                query.Search,
                query.CategoryId,
                query.BrandId,
                isPublished: false,
                missingGalleryImage: query.MissingGalleryImage,
                missingEnglishContent: query.MissingEnglishContent)
            .Where(product => dbContext.ProductMedia.Any(media =>
                media.ProductId == product.Id &&
                media.Role == ProductMediaRole.GalleryImage &&
                dbContext.MediaAssets.Any(asset =>
                    asset.Id == media.MediaAssetId &&
                    asset.Status == MediaStatus.Active &&
                    asset.AssetType == MediaAssetType.Image &&
                    asset.StorageKey != null &&
                    asset.StorageKey != string.Empty)))
            .Select(product => product.Id)
            .Take(5_001)
            .ToListAsync(cancellationToken);

        if (productIds.Count > 5_000)
        {
            throw new InvalidOperationException(ProductManagementErrorCodes.ProductBulkItemLimitExceeded);
        }

        return productIds;
    }

    private IQueryable<Product> FilterProducts(
        string language,
        string? search,
        Guid? categoryId,
        Guid? brandId,
        bool? isPublished,
        bool missingGalleryImage,
        bool missingEnglishContent)
    {
        var products = dbContext.Products
            .AsNoTracking()
            .Where(AdminProductRelationRules.SelectableProduct(language));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            if (value.Length > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(search));
            }

            products = products.Where(product =>
                product.SKU.Contains(value) ||
                product.Translations.Any(translation =>
                    translation.Name.Contains(value)));
        }

        if (categoryId.HasValue)
        {
            products = products.Where(product => product.Categories.Any(
                category => category.CategoryId == categoryId.Value));
        }

        if (brandId.HasValue)
        {
            products = products.Where(product => product.BrandId == brandId.Value);
        }

        if (isPublished.HasValue)
        {
            products = products.Where(product => product.IsPublished == isPublished.Value);
        }

        if (missingGalleryImage)
        {
            products = products.Where(product =>
                !dbContext.ProductMedia.Any(media =>
                    media.ProductId == product.Id &&
                    media.Role == ProductMediaRole.GalleryImage));
        }

        if (missingEnglishContent)
        {
            products = products.Where(product =>
                !product.Translations.Any(translation =>
                    translation.LanguageCode == "en"));
        }

        return products;
    }

    private static IOrderedQueryable<Product> OrderProducts(
        IQueryable<Product> products,
        string language,
        AdminProductSortBy sortBy,
        AdminSortDirection direction)
    {
        var descending = direction == AdminSortDirection.Descending;
        IOrderedQueryable<Product> ordered = sortBy switch
        {
            AdminProductSortBy.SKU => descending
                ? products.OrderByDescending(product => product.SKU)
                : products.OrderBy(product => product.SKU),
            AdminProductSortBy.UpdatedAt => descending
                ? products.OrderByDescending(product => product.UpdatedAt)
                : products.OrderBy(product => product.UpdatedAt),
            AdminProductSortBy.CreatedAt => descending
                ? products.OrderByDescending(product => product.CreatedAt)
                : products.OrderBy(product => product.CreatedAt),
            _ => descending
                ? products.OrderByDescending(product => product.Translations
                    .Where(translation => translation.LanguageCode == language)
                    .Select(translation => translation.Name)
                    .Single())
                : products.OrderBy(product => product.Translations
                    .Where(translation => translation.LanguageCode == language)
                    .Select(translation => translation.Name)
                    .Single()),
        };
        return ordered.ThenBy(product => product.Id);
    }

    public async Task<IReadOnlyList<AdminProductRelation>> GetRelationsAsync(
        Guid productId,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        var language = ValidateLanguage(languageCode);
        await EnsureProductExistsAsync(
            productId,
            language,
            cancellationToken);
        return await ProjectRelations(productId, language)
            .OrderBy(relation => relation.RelationType)
            .ThenBy(relation => relation.SortOrder)
            .ThenBy(relation => relation.RelatedName)
            .ThenBy(relation => relation.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminProductRelation> CreateAsync(
        CreateAdminProductRelationCommand command,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var language = ValidateLanguage(languageCode);
        if (command.SourceProductId == Guid.Empty ||
            command.TargetProductId == Guid.Empty ||
            !Enum.IsDefined(command.RelationType))
        {
            throw new ArgumentException("Valid products and relation type are required.");
        }

        if (command.RelationType is not (
            ProductRelationType.Similar or
            ProductRelationType.Complementary or
            ProductRelationType.Accessory or
            ProductRelationType.Alternative))
        {
            throw new ArgumentOutOfRangeException(nameof(command));
        }

        await EnsureProductExistsAsync(
            command.SourceProductId,
            language,
            cancellationToken);
        await EnsureProductExistsAsync(
            command.TargetProductId,
            language,
            cancellationToken);

        var existing = await dbContext.ProductRelations
            .SingleOrDefaultAsync(
                relation =>
                    relation.SourceProductId == command.SourceProductId &&
                    relation.TargetProductId == command.TargetProductId &&
                    relation.RelationType == command.RelationType,
                cancellationToken);
        var hasConflictingReverse = await dbContext.ProductRelations.AnyAsync(
            AdminProductRelationRules.ConflictingReverse(command),
            cancellationToken);
        if (hasConflictingReverse)
        {
            throw new ProductRelationConflictException(
                "A reverse active relation already covers this product pair.");
        }

        var resolution = AdminProductRelationRules.ResolveExisting(existing);
        if (resolution == ExistingProductRelationResolution.RejectActive)
        {
            throw new ProductRelationConflictException(
                "This active product relation already exists.");
        }

        if (resolution == ExistingProductRelationResolution.RejectAutomatic)
        {
            throw new ProductRelationConflictException(
                "An automatically managed relation already uses this combination.");
        }

        var relation = existing ??
            new ProductRelation(
                Guid.NewGuid(),
                command.SourceProductId,
                command.TargetProductId,
                command.RelationType,
                command.IsBidirectional,
                command.SortOrder,
                ProductRelationOrigin.Manual);
        if (existing is not null)
        {
            existing.Configure(
                command.IsBidirectional,
                command.SortOrder);
        }
        else
        {
            dbContext.ProductRelations.Add(relation);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ProductRelationConflictException(
                "The product relation conflicts with an existing record.");
        }

        return await ProjectRelation(relation.Id, language)
            .SingleAsync(cancellationToken);
    }

    public async Task DeactivateAsync(
        Guid relationId,
        CancellationToken cancellationToken = default)
    {
        var relation = await dbContext.ProductRelations
            .SingleOrDefaultAsync(
                item =>
                    item.Id == relationId &&
                    item.IsActive &&
                    item.Origin == ProductRelationOrigin.Manual,
                cancellationToken);
        if (relation is null)
        {
            throw new ProductRelationNotFoundException();
        }

        relation.SetActive(false);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<AdminProductRelation> ProjectRelations(
        Guid productId,
        string language) =>
        dbContext.ProductRelations
            .AsNoTracking()
            .Where(
                AdminProductRelationRules.VisibleManualRelationFor(
                    productId))
            .SelectMany(relation => dbContext.Products
                .Where(product =>
                    product.Id ==
                    (relation.SourceProductId == productId
                        ? relation.TargetProductId
                        : relation.SourceProductId) &&
                    !product.IsDeleted)
                .SelectMany(product => product.Translations
                    .Where(translation =>
                        translation.LanguageCode == language)
                    .Select(translation => new AdminProductRelation(
                        relation.Id,
                        relation.SourceProductId,
                        relation.TargetProductId,
                        product.Id,
                        product.SKU,
                        translation.Name,
                        relation.RelationType,
                        relation.IsBidirectional,
                        relation.TargetProductId == productId,
                        relation.SortOrder))));

    private IQueryable<AdminProductRelation> ProjectRelation(
        Guid relationId,
        string language) =>
        dbContext.ProductRelations
            .AsNoTracking()
            .Where(relation =>
                relation.Id == relationId &&
                relation.IsActive)
            .SelectMany(relation => dbContext.Products
                .Where(product =>
                    product.Id == relation.TargetProductId &&
                    !product.IsDeleted)
                .SelectMany(product => product.Translations
                    .Where(translation =>
                        translation.LanguageCode == language)
                    .Select(translation => new AdminProductRelation(
                        relation.Id,
                        relation.SourceProductId,
                        relation.TargetProductId,
                        product.Id,
                        product.SKU,
                        translation.Name,
                        relation.RelationType,
                        relation.IsBidirectional,
                        false,
                        relation.SortOrder))));

    private async Task EnsureProductExistsAsync(
        Guid productId,
        string languageCode,
        CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty ||
            !await dbContext.Products
                .Where(
                    AdminProductRelationRules.SelectableProduct(
                        languageCode))
                .AnyAsync(
                    product => product.Id == productId,
                    cancellationToken))
        {
            throw new CatalogProductNotFoundException();
        }
    }

    private static string ValidateLanguage(string languageCode)
    {
        if (languageCode is not ("tr" or "en"))
        {
            throw new ArgumentOutOfRangeException(nameof(languageCode));
        }

        return languageCode;
    }

    private static void ValidatePagination(int page, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 100);
    }
}
