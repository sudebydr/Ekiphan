using Ekiphan.Application.Catalog;
using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.Media;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Catalog;

internal sealed class CatalogQueryService(
    EkiphanDbContext dbContext,
    IPublicMediaUrlResolver mediaUrlResolver)
    : ICatalogQueryService
{
    public async Task<CatalogNavigation> GetNavigationAsync(
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        ValidateLanguage(languageCode);
        var language = languageCode.ToLowerInvariant();
        var sectionRows = await dbContext.ProductSections
            .AsNoTracking()
            .Where(section =>
                section.IsPublished &&
                section.Translations.Any(translation =>
                    translation.LanguageCode == language))
            .SelectMany(section => section.Translations
                .Where(translation => translation.LanguageCode == language)
                .Select(translation => new
                {
                    section.SortOrder,
                    section.Id,
                    section.Code,
                    translation.Name,
                    translation.Slug,
                }))
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var sections = sectionRows
            .Select(item => new CatalogSectionNavigationItem(
                item.Id,
                item.Code,
                item.Name,
                item.Slug))
            .ToArray();

        var sectionIds = sections.Select(section => section.Id).ToArray();
        var categoryRows = await dbContext.Categories
            .AsNoTracking()
            .Where(category =>
                category.IsPublished &&
                sectionIds.Contains(category.ProductSectionId) &&
                category.Translations.Any(translation =>
                    translation.LanguageCode == language))
            .SelectMany(category => category.Translations
                .Where(translation => translation.LanguageCode == language)
                .Select(translation => new
                {
                    category.SortOrder,
                    category.Id,
                    category.ProductSectionId,
                    category.ParentId,
                    translation.Name,
                    translation.Slug,
                    translation.MetaTitle,
                    translation.MetaDescription,
                    translation.CanonicalUrl,
                    translation.NoIndex,
                    translation.NoFollow,
                    translation.OpenGraphTitle,
                    translation.OpenGraphDescription,
                    OpenGraphImageKey = dbContext.MediaAssets
                        .Where(media => media.Id == translation.OpenGraphImageMediaId &&
                            media.Status == MediaStatus.Active &&
                            media.AssetType == MediaAssetType.Image)
                        .Select(media => media.StorageKey)
                        .SingleOrDefault(),
                }))
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var categories = categoryRows
            .Select(item => new CatalogCategoryNavigationItem(
                item.Id,
                item.ProductSectionId,
                item.ParentId,
                item.Name,
                item.Slug,
                item.MetaTitle,
                item.MetaDescription,
                item.CanonicalUrl,
                item.NoIndex,
                item.NoFollow,
                item.OpenGraphTitle,
                item.OpenGraphDescription,
                mediaUrlResolver.Resolve(item.OpenGraphImageKey)))
            .ToArray();

        var brandRows = await dbContext.Brands
            .AsNoTracking()
            .Where(brand =>
                brand.IsPublished &&
                brand.Translations.Any(translation =>
                    translation.LanguageCode == language))
            .Select(brand => new
            {
                brand.SortOrder,
                brand.Id,
                brand.Name,
                Slug = brand.Translations
                    .Where(translation =>
                        translation.LanguageCode == language)
                    .Select(translation => translation.Slug)
                    .Single(),
            })
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var brands = brandRows
            .Select(item => new CatalogBrandSummary(
                item.Id,
                item.Name,
                item.Slug))
            .ToArray();

        var tagRows = await dbContext.Tags
            .AsNoTracking()
            .Where(tag =>
                tag.IsActive &&
                tag.Translations.Any(translation =>
                    translation.LanguageCode == language))
            .Select(tag => new
            {
                tag.Id,
                tag.Code,
                Name = tag.Translations
                    .Where(translation =>
                        translation.LanguageCode == language)
                    .Select(translation => translation.Name)
                    .Single(),
                Slug = tag.Translations
                    .Where(translation =>
                        translation.LanguageCode == language)
                    .Select(translation => translation.Slug)
                    .Single(),
            })
            .OrderBy(tag => tag.Name)
            .ThenBy(tag => tag.Id)
            .ToListAsync(cancellationToken);
        var tags = tagRows
            .Select(item => new CatalogTagSummary(
                item.Id,
                item.Code,
                item.Name,
                item.Slug))
            .ToArray();

        return new CatalogNavigation(sections, categories, brands, tags);
    }

    public async Task<IReadOnlyList<CatalogBrandListItem>> GetBrandsAsync(
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        ValidateLanguage(languageCode);
        var language = languageCode.ToLowerInvariant();
        var rows = await dbContext.Brands
            .AsNoTracking()
            .Where(brand =>
                brand.IsPublished &&
                brand.Translations.Any(translation =>
                    translation.LanguageCode == language))
            .SelectMany(brand => brand.Translations
                .Where(translation =>
                    translation.LanguageCode == language)
                .Select(translation => new
                {
                    brand.Id,
                    brand.Name,
                    translation.Slug,
                    translation.Description,
                    brand.WebsiteUrl,
                    brand.SortOrder,
                    brand.UpdatedAt,
                }))
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .ThenBy(item => item.Id)
            .Take(500)
            .ToListAsync(cancellationToken);
        var logos = await GetBrandLogosAsync(
            rows.Select(item => item.Id).ToArray(),
            language,
            cancellationToken);

        return rows
            .Select(item => new CatalogBrandListItem(
                item.Id,
                item.Name,
                item.Slug,
                item.Description,
                item.WebsiteUrl,
                logos.GetValueOrDefault(item.Id),
                item.UpdatedAt))
            .ToArray();
    }

    public async Task<CatalogBrandDetail?> GetBrandAsync(
        string languageCode,
        string slug,
        CancellationToken cancellationToken = default)
    {
        ValidateLanguage(languageCode);
        ValidateText(slug, 200, nameof(slug));
        var language = languageCode.ToLowerInvariant();
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var brand = await dbContext.Brands
            .AsNoTracking()
            .Where(item =>
                item.IsPublished &&
                item.Translations.Any(translation =>
                    translation.LanguageCode == language &&
                    translation.Slug == normalizedSlug))
            .SelectMany(item => item.Translations
                .Where(translation =>
                    translation.LanguageCode == language &&
                    translation.Slug == normalizedSlug)
                .Select(translation => new
                {
                    item.Id,
                    item.Name,
                    translation.Slug,
                    translation.Description,
                    item.WebsiteUrl,
                    item.UpdatedAt,
                }))
            .SingleOrDefaultAsync(cancellationToken);
        if (brand is null)
        {
            return null;
        }

        var products = await dbContext.Products
            .AsNoTracking()
            .Where(product =>
                product.BrandId == brand.Id &&
                product.IsPublished &&
                product.Translations.Any(translation =>
                    translation.LanguageCode == language))
            .OrderBy(product => product.Translations
                .Where(translation =>
                    translation.LanguageCode == language)
                .Select(translation => translation.Name)
                .Single())
            .ThenBy(product => product.Id)
            .Select(product => new CatalogProductSummary(
                product.Id,
                product.SKU,
                product.Translations
                    .Where(translation =>
                        translation.LanguageCode == language)
                    .Select(translation => translation.Name)
                    .Single(),
                product.Translations
                    .Where(translation =>
                        translation.LanguageCode == language)
                    .Select(translation => translation.Slug)
                    .Single(),
                product.Translations
                    .Where(translation =>
                        translation.LanguageCode == language)
                    .Select(translation => translation.ShortDescription)
                    .Single(),
                new CatalogBrandSummary(
                    brand.Id,
                    brand.Name,
                    brand.Slug),
                dbContext.Categories
                    .Where(category =>
                        category.Id == product.PrimaryCategoryId &&
                        category.IsPublished)
                    .SelectMany(category => category.Translations
                        .Where(translation =>
                            translation.LanguageCode == language)
                        .Select(translation =>
                            new CatalogCategorySummary(
                                category.Id,
                                translation.Name,
                                translation.Slug)))
                    .SingleOrDefault(),
                product.UpdatedAt,
                null))
            .Take(24)
            .ToListAsync(cancellationToken);
        var productImages = await GetProductImagesAsync(
            products.Select(item => item.Id).ToArray(),
            language,
            cancellationToken);
        products = products
            .Select(item => item with
            {
                Image = productImages.TryGetValue(
                    item.Id,
                    out var images)
                    ? images[0]
                    : null,
            })
            .ToList();

        var categoryRows = await dbContext.Products
            .AsNoTracking()
            .Where(product =>
                product.BrandId == brand.Id &&
                product.IsPublished)
            .SelectMany(product => product.Categories)
            .Join(
                dbContext.Categories.Where(category =>
                    category.IsPublished),
                productCategory => productCategory.CategoryId,
                category => category.Id,
                (productCategory, category) => category)
            .SelectMany(category => category.Translations
                .Where(translation =>
                    translation.LanguageCode == language)
                .Select(translation => new
                {
                    category.Id,
                    translation.Name,
                    translation.Slug,
                }))
            .Distinct()
            .OrderBy(item => item.Name)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
        var categories = categoryRows
            .Select(item => new CatalogCategorySummary(
                item.Id,
                item.Name,
                item.Slug))
            .ToArray();
        var logos = await GetBrandLogosAsync(
            [brand.Id],
            language,
            cancellationToken);
        var catalogs = await GetBrandCatalogsAsync(
            brand.Id,
            language,
            cancellationToken);

        return new CatalogBrandDetail(
            brand.Id,
            brand.Name,
            brand.Slug,
            brand.Description,
            brand.WebsiteUrl,
            logos.GetValueOrDefault(brand.Id),
            catalogs,
            categories,
            products,
            brand.UpdatedAt);
    }

    public async Task<CatalogPagedResult<CatalogProductSummary>> GetProductsAsync(
        CatalogProductQuery request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);
        var language = request.LanguageCode.ToLowerInvariant();
        var products = dbContext.Products
            .AsNoTracking()
            .Where(product =>
                product.IsPublished &&
                product.Translations.Any(translation =>
                    translation.LanguageCode == language));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            products = products.Where(product =>
                product.SKU.Contains(search) ||
                product.Translations.Any(translation =>
                    translation.LanguageCode == language &&
                    (translation.Name.Contains(search) ||
                     translation.ShortDescription != null &&
                     translation.ShortDescription.Contains(search))));
        }

        if (!string.IsNullOrWhiteSpace(request.SectionSlug))
        {
            var slug = request.SectionSlug.Trim().ToLowerInvariant();
            products = products.Where(product =>
                product.Categories.Any(productCategory =>
                    dbContext.Categories.Any(category =>
                        category.Id == productCategory.CategoryId &&
                        category.IsPublished &&
                        dbContext.ProductSections.Any(section =>
                            section.Id == category.ProductSectionId &&
                            section.IsPublished &&
                            section.Translations.Any(translation =>
                                translation.LanguageCode == language &&
                                translation.Slug == slug)))));
        }

        if (!string.IsNullOrWhiteSpace(request.CategorySlug))
        {
            var slug = request.CategorySlug.Trim().ToLowerInvariant();
            products = products.Where(product =>
                product.Categories.Any(productCategory =>
                    dbContext.Categories.Any(category =>
                        category.Id == productCategory.CategoryId &&
                        category.IsPublished &&
                        category.Translations.Any(translation =>
                            translation.LanguageCode == language &&
                            translation.Slug == slug))));
        }

        if (!string.IsNullOrWhiteSpace(request.BrandSlug))
        {
            var slug = request.BrandSlug.Trim().ToLowerInvariant();
            products = products.Where(product =>
                product.BrandId.HasValue &&
                dbContext.Brands.Any(brand =>
                    brand.Id == product.BrandId &&
                    brand.IsPublished &&
                    brand.Translations.Any(translation =>
                        translation.LanguageCode == language &&
                        translation.Slug == slug)));
        }

        if (!string.IsNullOrWhiteSpace(request.TagSlug))
        {
            var slug = request.TagSlug.Trim().ToLowerInvariant();
            products = products.Where(product =>
                product.Tags.Any(productTag =>
                    dbContext.Tags.Any(tag =>
                        tag.Id == productTag.TagId &&
                        tag.IsActive &&
                        tag.Translations.Any(translation =>
                            translation.LanguageCode == language &&
                            translation.Slug == slug))));
        }

        products = ApplyAttributeFilters(products, request.AttributeFilters);

        var totalCount = await products.CountAsync(cancellationToken);
        products = request.Sort switch
        {
            CatalogProductSort.NameDescending =>
                products.OrderByDescending(product => product.Translations
                        .Where(translation =>
                            translation.LanguageCode == language)
                        .Select(translation => translation.Name)
                        .Single())
                    .ThenBy(product => product.Id),
            CatalogProductSort.Newest =>
                products.OrderByDescending(product => product.UpdatedAt)
                    .ThenBy(product => product.Id),
            _ => products.OrderBy(product => product.Translations
                    .Where(translation =>
                        translation.LanguageCode == language)
                    .Select(translation => translation.Name)
                    .Single())
                .ThenBy(product => product.Id),
        };

        var projected = products.Select(product => new CatalogProductSummary(
            product.Id,
            product.SKU,
            product.Translations
                .Where(translation => translation.LanguageCode == language)
                .Select(translation => translation.Name)
                .Single(),
            product.Translations
                .Where(translation => translation.LanguageCode == language)
                .Select(translation => translation.Slug)
                .Single(),
            product.Translations
                .Where(translation => translation.LanguageCode == language)
                .Select(translation => translation.ShortDescription)
                .Single(),
            dbContext.Brands
                .Where(brand =>
                    brand.Id == product.BrandId &&
                    brand.IsPublished)
                .Select(brand => new CatalogBrandSummary(
                    brand.Id,
                    brand.Name,
                    brand.Translations
                        .Where(translation =>
                            translation.LanguageCode == language)
                        .Select(translation => translation.Slug)
                        .SingleOrDefault()))
                .SingleOrDefault(),
            dbContext.Categories
                .Where(category =>
                    category.Id == product.PrimaryCategoryId &&
                    category.IsPublished)
                .Select(category => category.Translations
                    .Where(translation =>
                        translation.LanguageCode == language)
                    .Select(translation => new CatalogCategorySummary(
                        category.Id,
                        translation.Name,
                        translation.Slug))
                    .SingleOrDefault())
                .SingleOrDefault(),
            product.UpdatedAt,
            null));

        var items = await projected
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var images = await GetProductImagesAsync(
            items.Select(item => item.Id).ToArray(),
            language,
            cancellationToken);
        items = items
            .Select(item => item with
            {
                Image = images.TryGetValue(item.Id, out var productImages)
                    ? productImages[0]
                    : null,
            })
            .ToList();

        return new CatalogPagedResult<CatalogProductSummary>(
            items,
            request.Page,
            request.PageSize,
            totalCount);
    }

    public async Task<IReadOnlyList<CatalogFacet>> GetFacetsAsync(
        string languageCode,
        string categorySlug,
        CancellationToken cancellationToken = default)
    {
        ValidateLanguage(languageCode);
        ValidateText(categorySlug, 250, nameof(categorySlug));
        var language = languageCode.ToLowerInvariant();
        var slug = categorySlug.Trim().ToLowerInvariant();
        var categoryId = await dbContext.Categories.AsNoTracking()
            .Where(category => category.IsPublished &&
                category.Translations.Any(text =>
                    text.LanguageCode == language && text.Slug == slug))
            .Select(category => (Guid?)category.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (!categoryId.HasValue)
        {
            return [];
        }

        var definitions = await dbContext.CategoryAttributes.AsNoTracking()
            .Where(assignment => assignment.CategoryId == categoryId &&
                assignment.IsFilterable)
            .Join(dbContext.Attributes.AsNoTracking().Where(attribute =>
                    attribute.IsActive &&
                    attribute.Translations.Any(text => text.LanguageCode == language)),
                assignment => assignment.AttributeId,
                attribute => attribute.Id,
                (assignment, attribute) => new
                {
                    attribute.Id,
                    attribute.Code,
                    attribute.DataType,
                    assignment.SortOrder,
                    Name = attribute.Translations.Where(text =>
                        text.LanguageCode == language).Select(text => text.Name).Single()
                })
            .OrderBy(item => item.SortOrder).ThenBy(item => item.Name)
            .Take(50)
            .ToListAsync(cancellationToken);
        if (definitions.Count == 0)
        {
            return [];
        }

        var attributeIds = definitions.Select(item => item.Id).ToArray();
        var values = await dbContext.ProductAttributeValues.AsNoTracking()
            .Where(value => attributeIds.Contains(value.AttributeId) &&
                dbContext.Products.Any(product =>
                    product.Id == value.ProductId &&
                    product.IsPublished &&
                    product.Translations.Any(text => text.LanguageCode == language) &&
                    product.Categories.Any(link => link.CategoryId == categoryId)))
            .Select(value => new
            {
                value.AttributeId,
                value.TextValue,
                value.NumericValue,
                value.BooleanValue,
                value.AttributeOptionId,
                OptionName = dbContext.Set<AttributeOption>()
                    .Where(option => option.Id == value.AttributeOptionId && option.IsActive)
                    .Select(option => option.Translations.Where(text =>
                        text.LanguageCode == language).Select(text => text.Name)
                        .SingleOrDefault()).SingleOrDefault(),
                UnitSymbol = dbContext.Units.Where(unit => unit.Id == value.UnitId)
                    .Select(unit => unit.Symbol).SingleOrDefault()
            })
            .ToListAsync(cancellationToken);

        return definitions.Select(definition =>
        {
            var facetValues = values.Where(value =>
                value.AttributeId == definition.Id).ToArray();
            var options = definition.DataType switch
            {
                AttributeDataType.Boolean => facetValues
                    .Where(value => value.BooleanValue.HasValue)
                    .Select(value => value.BooleanValue!.Value)
                    .Distinct()
                    .OrderByDescending(value => value)
                    .Select(value => new CatalogFacetOption(
                        $"b:{value.ToString().ToLowerInvariant()}",
                        value ? "Evet" : "Hayır"))
                    .ToArray(),
                AttributeDataType.Option or AttributeDataType.MultiOption => facetValues
                    .Where(value => value.AttributeOptionId.HasValue &&
                        value.OptionName != null)
                    .Select(value => new
                    {
                        Id = value.AttributeOptionId!.Value,
                        Name = value.OptionName!
                    })
                    .Distinct()
                    .OrderBy(value => value.Name)
                    .Select(value => new CatalogFacetOption($"o:{value.Id}", value.Name))
                    .ToArray(),
                AttributeDataType.Text => facetValues
                    .Where(value => !string.IsNullOrWhiteSpace(value.TextValue))
                    .Select(value => value.TextValue!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value)
                    .Take(100)
                    .Select(value => new CatalogFacetOption($"t:{value}", value))
                    .ToArray(),
                _ => [],
            };
            return new CatalogFacet(
                definition.Id,
                definition.Code,
                definition.Name,
                definition.DataType,
                facetValues.Select(value => value.UnitSymbol)
                    .FirstOrDefault(symbol => symbol != null),
                definition.DataType == AttributeDataType.Number
                    ? facetValues.Min(value => value.NumericValue)
                    : null,
                definition.DataType == AttributeDataType.Number
                    ? facetValues.Max(value => value.NumericValue)
                    : null,
                options);
        }).Where(facet => facet.Options.Count > 0 ||
            facet.Minimum.HasValue || facet.Maximum.HasValue).ToArray();
    }

    private IQueryable<Product> ApplyAttributeFilters(
        IQueryable<Product> products,
        IReadOnlyList<CatalogAttributeFilter>? filters)
    {
        foreach (var filter in filters ?? [])
        {
            var token = filter.Value;
            if (token.StartsWith("o:", StringComparison.Ordinal) &&
                Guid.TryParse(token[2..], out var optionId))
            {
                products = products.Where(product =>
                    dbContext.ProductAttributeValues.Any(value =>
                        value.ProductId == product.Id &&
                        value.AttributeId == filter.AttributeId &&
                        value.AttributeOptionId == optionId));
            }
            else if (token.StartsWith("b:", StringComparison.Ordinal) &&
                bool.TryParse(token[2..], out var booleanValue))
            {
                products = products.Where(product =>
                    dbContext.ProductAttributeValues.Any(value =>
                        value.ProductId == product.Id &&
                        value.AttributeId == filter.AttributeId &&
                        value.BooleanValue == booleanValue));
            }
            else if (token.StartsWith("t:", StringComparison.Ordinal) &&
                token.Length > 2)
            {
                var textValue = token[2..];
                products = products.Where(product =>
                    dbContext.ProductAttributeValues.Any(value =>
                        value.ProductId == product.Id &&
                        value.AttributeId == filter.AttributeId &&
                        value.TextValue == textValue));
            }
            else if (TryParseNumberRange(token, out var minimum, out var maximum))
            {
                products = products.Where(product =>
                    dbContext.ProductAttributeValues.Any(value =>
                        value.ProductId == product.Id &&
                        value.AttributeId == filter.AttributeId &&
                        value.NumericValue.HasValue &&
                        (!minimum.HasValue || value.NumericValue >= minimum) &&
                        (!maximum.HasValue || value.NumericValue <= maximum)));
            }
            else
            {
                throw new ArgumentException("Attribute filter value is invalid.");
            }
        }
        return products;
    }

    private static bool TryParseNumberRange(
        string token,
        out decimal? minimum,
        out decimal? maximum)
    {
        minimum = null;
        maximum = null;
        if (!token.StartsWith("n:", StringComparison.Ordinal))
        {
            return false;
        }
        var parts = token[2..].Split(':');
        decimal min = 0;
        decimal max = 0;
        if (parts.Length != 2 ||
            (!string.IsNullOrEmpty(parts[0]) &&
                !decimal.TryParse(parts[0], System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out min)) ||
            (!string.IsNullOrEmpty(parts[1]) &&
                !decimal.TryParse(parts[1], System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out max)))
        {
            return false;
        }
        if (!string.IsNullOrEmpty(parts[0])) minimum = min;
        if (!string.IsNullOrEmpty(parts[1])) maximum = max;
        return (minimum.HasValue || maximum.HasValue) &&
            (!minimum.HasValue || !maximum.HasValue || minimum <= maximum);
    }

    public async Task<IReadOnlyList<CatalogSitemapEntry>>
        GetSitemapEntriesAsync(
            string languageCode,
            CancellationToken cancellationToken = default)
    {
        ValidateLanguage(languageCode);
        var language = languageCode.ToLowerInvariant();

        var products = await dbContext.Products
            .AsNoTracking()
            .Where(product =>
                product.IsPublished &&
                product.Translations.Any(translation =>
                    translation.LanguageCode == language &&
                    !translation.NoIndex))
            .SelectMany(product => product.Translations
                .Where(translation => translation.LanguageCode == language &&
                    !translation.NoIndex)
                .Select(translation => new CatalogSitemapEntry(
                    translation.Slug,
                    product.UpdatedAt,
                    "product")))
            .OrderBy(item => item.Slug)
            .Take(CatalogSitemapLimits.MaximumProductUrls)
            .ToListAsync(cancellationToken);
        var categories = await dbContext.Categories.AsNoTracking()
            .Where(category => category.IsPublished)
            .SelectMany(category => category.Translations
                .Where(translation => translation.LanguageCode == language &&
                    !translation.NoIndex)
                .Select(translation => new CatalogSitemapEntry(
                    translation.Slug, category.UpdatedAt, "category")))
            .OrderBy(item => item.Slug)
            .Take(5_000)
            .ToListAsync(cancellationToken);
        return products.Concat(categories).ToArray();
    }

    public async Task<CatalogProductDetail?> GetProductAsync(
        string languageCode,
        string slug,
        CancellationToken cancellationToken = default)
    {
        ValidateLanguage(languageCode);
        ValidateText(slug, 300, nameof(slug));
        var language = languageCode.ToLowerInvariant();
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        var product = await dbContext.Products
            .AsNoTracking()
            .Where(item =>
                item.IsPublished &&
                item.Translations.Any(translation =>
                    translation.LanguageCode == language &&
                    translation.Slug == normalizedSlug))
            .Select(item => new
            {
                item.Id,
                item.SKU,
                item.BrandId,
                item.UpdatedAt,
                Translation = item.Translations
                    .Where(translation =>
                        translation.LanguageCode == language)
                    .Select(translation => new
                    {
                        translation.Name,
                        translation.Slug,
                        translation.ShortDescription,
                        translation.LongDescription,
                        translation.MetaTitle,
                        translation.MetaDescription,
                        translation.CanonicalUrl,
                        translation.NoIndex,
                        translation.NoFollow,
                        translation.OpenGraphTitle,
                        translation.OpenGraphDescription,
                        OpenGraphImageKey = dbContext.MediaAssets
                            .Where(media => media.Id == translation.OpenGraphImageMediaId &&
                                media.Status == MediaStatus.Active &&
                                media.AssetType == MediaAssetType.Image)
                            .Select(media => media.StorageKey)
                            .SingleOrDefault(),
                    })
                    .Single(),
                Alternates = item.Translations
                    .OrderBy(translation => translation.LanguageCode)
                    .Select(translation => new CatalogSeoAlternate(
                        translation.LanguageCode, translation.Slug))
                    .ToArray(),
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (product is null)
        {
            return null;
        }

        var brand = await dbContext.Brands
            .AsNoTracking()
            .Where(item =>
                item.Id == product.BrandId &&
                item.IsPublished)
            .Select(item => new CatalogBrandSummary(
                item.Id,
                item.Name,
                item.Translations
                    .Where(translation =>
                        translation.LanguageCode == language)
                    .Select(translation => translation.Slug)
                    .SingleOrDefault()))
            .SingleOrDefaultAsync(cancellationToken);

        var categoryRows = await dbContext.Products
            .AsNoTracking()
            .Where(item => item.Id == product.Id)
            .SelectMany(item => item.Categories)
            .Join(
                dbContext.Categories.Where(category => category.IsPublished),
                productCategory => productCategory.CategoryId,
                category => category.Id,
                (productCategory, category) => new
                {
                    productCategory.SortOrder,
                    category.Id,
                    category.Translations,
                })
            .SelectMany(item => item.Translations
                .Where(translation =>
                    translation.LanguageCode == language)
                .Select(translation => new
                {
                    item.SortOrder,
                    item.Id,
                    translation.Name,
                    translation.Slug,
                }))
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var categories = categoryRows
            .Select(item => new CatalogCategorySummary(
                item.Id,
                item.Name,
                item.Slug))
            .ToArray();

        var tagRows = await dbContext.Products
            .AsNoTracking()
            .Where(item => item.Id == product.Id)
            .SelectMany(item => item.Tags)
            .Join(
                dbContext.Tags.Where(tag => tag.IsActive),
                productTag => productTag.TagId,
                tag => tag.Id,
                (productTag, tag) => new
                {
                    productTag.SortOrder,
                    tag.Id,
                    tag.Code,
                    tag.Translations,
                })
            .SelectMany(item => item.Translations
                .Where(translation =>
                    translation.LanguageCode == language)
                .Select(translation => new
                {
                    item.SortOrder,
                    item.Id,
                    item.Code,
                    translation.Name,
                    translation.Slug,
                }))
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var tags = tagRows
            .Select(item => new CatalogTagSummary(
                item.Id,
                item.Code,
                item.Name,
                item.Slug))
            .ToArray();

        var attributes = await dbContext.ProductAttributeValues
            .AsNoTracking()
            .Where(value => value.ProductId == product.Id)
            .Join(
                dbContext.Attributes.Where(attribute =>
                    attribute.IsActive &&
                    attribute.Translations.Any(translation =>
                        translation.LanguageCode == language)),
                value => value.AttributeId,
                attribute => attribute.Id,
                (value, attribute) => new
                {
                    Value = value,
                    Attribute = attribute,
                })
            .OrderBy(item => item.Attribute.Translations
                .Where(translation =>
                    translation.LanguageCode == language)
                .Select(translation => translation.Name)
                .Single())
            .ThenBy(item => item.Value.Sequence)
            .Select(item => new CatalogAttributeValue(
                item.Value.Id,
                item.Attribute.Id,
                item.Attribute.Translations
                    .Where(translation =>
                        translation.LanguageCode == language)
                    .Select(translation => translation.Name)
                    .Single(),
                item.Attribute.DataType,
                item.Value.Sequence,
                item.Value.TextValue,
                item.Value.NumericValue,
                item.Value.BooleanValue,
                item.Value.AttributeOptionId,
                dbContext.Set<Domain.Catalog.AttributeOption>()
                    .Where(option =>
                        option.Id == item.Value.AttributeOptionId &&
                        option.IsActive)
                    .Select(option => option.Translations
                        .Where(translation =>
                            translation.LanguageCode == language)
                        .Select(translation => translation.Name)
                        .SingleOrDefault())
                    .SingleOrDefault(),
                item.Value.UnitId,
                dbContext.Units
                    .Where(unit => unit.Id == item.Value.UnitId)
                    .Select(unit => unit.Symbol)
                    .SingleOrDefault()))
            .ToListAsync(cancellationToken);

        var similarProducts = await GetRelatedProductsAsync(
            product.Id,
            ProductRelationType.Similar,
            language,
            cancellationToken);
        var complementaryProducts = await GetRelatedProductsAsync(
            product.Id,
            ProductRelationType.Complementary,
            language,
            cancellationToken);
        var images = await GetProductImagesAsync(
            [product.Id],
            language,
            cancellationToken);

        return new CatalogProductDetail(
            product.Id,
            product.SKU,
            product.Translation.Name,
            product.Translation.Slug,
            product.Translation.ShortDescription,
            product.Translation.LongDescription,
            brand,
            categories,
            tags,
            attributes,
            similarProducts,
            complementaryProducts,
            images.TryGetValue(product.Id, out var productImages)
                ? productImages
                : [],
            product.UpdatedAt,
            product.Translation.MetaTitle,
            product.Translation.MetaDescription,
            product.Translation.CanonicalUrl,
            product.Translation.NoIndex,
            product.Translation.NoFollow,
            product.Translation.OpenGraphTitle,
            product.Translation.OpenGraphDescription,
            mediaUrlResolver.Resolve(product.Translation.OpenGraphImageKey),
            product.Alternates);
    }

    private async Task<IReadOnlyList<CatalogProductSummary>>
        GetRelatedProductsAsync(
            Guid productId,
            ProductRelationType relationType,
            string language,
            CancellationToken cancellationToken)
    {
        const int maximumRelatedProducts = 12;
        var relations = await dbContext.ProductRelations
            .AsNoTracking()
            .Where(relation =>
                relation.IsActive &&
                relation.Origin == ProductRelationOrigin.Manual &&
                relation.RelationType == relationType &&
                (relation.SourceProductId == productId ||
                 relation.IsBidirectional &&
                 relation.TargetProductId == productId))
            .Select(relation => new
            {
                RelatedProductId = relation.SourceProductId == productId
                    ? relation.TargetProductId
                    : relation.SourceProductId,
                relation.SortOrder,
                relation.Id,
            })
            .OrderBy(relation => relation.SortOrder)
            .ThenBy(relation => relation.Id)
            .Take(maximumRelatedProducts)
            .ToListAsync(cancellationToken);

        if (relations.Count == 0)
        {
            return [];
        }

        var relatedIds = relations
            .Select(relation => relation.RelatedProductId)
            .Distinct()
            .ToArray();
        var products = await dbContext.Products
            .AsNoTracking()
            .Where(item =>
                relatedIds.Contains(item.Id) &&
                item.IsPublished &&
                item.Translations.Any(translation =>
                    translation.LanguageCode == language))
            .Select(item => new CatalogProductSummary(
                item.Id,
                item.SKU,
                item.Translations
                    .Where(translation =>
                        translation.LanguageCode == language)
                    .Select(translation => translation.Name)
                    .Single(),
                item.Translations
                    .Where(translation =>
                        translation.LanguageCode == language)
                    .Select(translation => translation.Slug)
                    .Single(),
                item.Translations
                    .Where(translation =>
                        translation.LanguageCode == language)
                    .Select(translation => translation.ShortDescription)
                    .Single(),
                dbContext.Brands
                    .Where(brand =>
                        brand.Id == item.BrandId &&
                        brand.IsPublished)
                    .Select(brand => new CatalogBrandSummary(
                        brand.Id,
                        brand.Name,
                        brand.Translations
                            .Where(translation =>
                                translation.LanguageCode == language)
                            .Select(translation => translation.Slug)
                            .SingleOrDefault()))
                    .SingleOrDefault(),
                dbContext.Categories
                    .Where(category =>
                        category.Id == item.PrimaryCategoryId &&
                        category.IsPublished)
                    .Select(category => category.Translations
                        .Where(translation =>
                            translation.LanguageCode == language)
                        .Select(translation => new CatalogCategorySummary(
                            category.Id,
                            translation.Name,
                            translation.Slug))
                        .SingleOrDefault())
                    .SingleOrDefault(),
                item.UpdatedAt,
                null))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        var orderedProducts = relations
            .Where(relation => products.ContainsKey(
                relation.RelatedProductId))
            .Select(relation => products[relation.RelatedProductId])
            .DistinctBy(item => item.Id)
            .ToArray();
        var images = await GetProductImagesAsync(
            orderedProducts.Select(item => item.Id).ToArray(),
            language,
            cancellationToken);

        return orderedProducts
            .Select(item => item with
            {
                Image = images.TryGetValue(item.Id, out var productImages)
                    ? productImages[0]
                    : null,
            })
            .ToArray();
    }

    private async Task<Dictionary<Guid, IReadOnlyList<CatalogImage>>>
        GetProductImagesAsync(
            Guid[] productIds,
            string language,
            CancellationToken cancellationToken)
    {
        if (productIds.Length == 0)
        {
            return [];
        }

        var imageSources = await dbContext.ProductMedia
            .AsNoTracking()
            .Where(productMedia =>
                productIds.Contains(productMedia.ProductId) &&
                productMedia.Role == ProductMediaRole.GalleryImage)
            .Join(
                dbContext.MediaAssets.Where(asset =>
                    asset.Status == MediaStatus.Active &&
                    asset.AssetType == MediaAssetType.Image &&
                    asset.StorageKey != null),
                productMedia => productMedia.MediaAssetId,
                asset => asset.Id,
                (productMedia, asset) => new
                {
                    productMedia.ProductId,
                    productMedia.IsDefault,
                    productMedia.SortOrder,
                    Asset = asset,
                })
            .SelectMany(item => item.Asset.Translations
                .Where(translation =>
                    translation.LanguageCode == language &&
                    translation.AltText != null)
                .Select(translation => new
                {
                    item.ProductId,
                    Id = item.Asset.Id,
                    StorageKey = item.Asset.StorageKey!,
                    AltText = translation.AltText!,
                    item.IsDefault,
                    item.SortOrder,
                }))
            .OrderBy(source => source.ProductId)
            .ThenByDescending(source => source.IsDefault)
            .ThenBy(source => source.SortOrder)
            .ThenBy(source => source.Id)
            .ToListAsync(cancellationToken);

        return imageSources
            .Select(source => new
            {
                Source = source,
                Url = mediaUrlResolver.Resolve(source.StorageKey),
            })
            .Where(item => item.Url is not null)
            .GroupBy(item => item.Source.ProductId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<CatalogImage>)group
                    .Select(item => new CatalogImage(
                        item.Source.Id,
                        item.Url!,
                        item.Source.AltText))
                    .ToArray());
    }

    private async Task<Dictionary<Guid, CatalogImage>> GetBrandLogosAsync(
        Guid[] brandIds,
        string language,
        CancellationToken cancellationToken)
    {
        if (brandIds.Length == 0)
        {
            return [];
        }

        var sources = await dbContext.BrandMedia
            .AsNoTracking()
            .Where(item =>
                brandIds.Contains(item.BrandId) &&
                item.Role == BrandMediaRole.Logo)
            .Join(
                dbContext.MediaAssets.Where(asset =>
                    asset.Status == MediaStatus.Active &&
                    asset.AssetType == MediaAssetType.Image &&
                    asset.StorageKey != null),
                item => item.MediaAssetId,
                asset => asset.Id,
                (item, asset) => new
                {
                    item.BrandId,
                    asset.Id,
                    StorageKey = asset.StorageKey!,
                    asset.Translations,
                })
            .SelectMany(item => item.Translations
                .Where(translation =>
                    translation.LanguageCode == language &&
                    translation.AltText != null)
                .Select(translation => new
                {
                    item.BrandId,
                    item.Id,
                    item.StorageKey,
                    AltText = translation.AltText!,
                }))
            .ToListAsync(cancellationToken);

        return sources
            .Select(source => new
            {
                source.BrandId,
                source.Id,
                source.AltText,
                Url = mediaUrlResolver.Resolve(source.StorageKey),
            })
            .Where(item => item.Url is not null)
            .ToDictionary(
                item => item.BrandId,
                item => new CatalogImage(
                    item.Id,
                    item.Url!,
                    item.AltText));
    }

    private async Task<IReadOnlyList<CatalogDocument>> GetBrandCatalogsAsync(
        Guid brandId,
        string language,
        CancellationToken cancellationToken)
    {
        var sources = await dbContext.BrandMedia
            .AsNoTracking()
            .Where(item =>
                item.BrandId == brandId &&
                item.Role == BrandMediaRole.PdfCatalog)
            .Join(
                dbContext.MediaAssets.Where(asset =>
                    asset.Status == MediaStatus.Active &&
                    asset.AssetType == MediaAssetType.Pdf &&
                    asset.StorageKey != null),
                item => item.MediaAssetId,
                asset => asset.Id,
                (item, asset) => new
                {
                    item.SortOrder,
                    asset.Id,
                    StorageKey = asset.StorageKey!,
                    asset.Translations,
                })
            .SelectMany(item => item.Translations
                .Where(translation =>
                    translation.LanguageCode == language)
                .Select(translation => new
                {
                    item.SortOrder,
                    item.Id,
                    item.StorageKey,
                    translation.Title,
                }))
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Title)
            .ToListAsync(cancellationToken);

        return sources
            .Select(source => new
            {
                source.Id,
                source.Title,
                Url = mediaUrlResolver.Resolve(source.StorageKey),
            })
            .Where(item => item.Url is not null)
            .Select(item => new CatalogDocument(
                item.Id,
                item.Url!,
                item.Title))
            .ToArray();
    }

    private static void Validate(CatalogProductQuery request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateLanguage(request.LanguageCode);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.Page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.PageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(request.PageSize, 100);
        ValidateOptionalText(request.Search, 100, nameof(request.Search));
        ValidateOptionalText(request.SectionSlug, 200, nameof(request.SectionSlug));
        ValidateOptionalText(request.CategorySlug, 250, nameof(request.CategorySlug));
        ValidateOptionalText(request.BrandSlug, 200, nameof(request.BrandSlug));
        ValidateOptionalText(request.TagSlug, 200, nameof(request.TagSlug));
        if (!Enum.IsDefined(request.Sort))
        {
            throw new ArgumentOutOfRangeException(nameof(request));
        }
    }

    private static void ValidateLanguage(string languageCode)
    {
        if (languageCode is not ("tr" or "en"))
        {
            throw new ArgumentOutOfRangeException(
                nameof(languageCode),
                "Only tr and en are supported.");
        }
    }

    private static void ValidateOptionalText(
        string? value,
        int maximumLength,
        string parameterName)
    {
        if (value is not null)
        {
            ValidateText(value, maximumLength, parameterName);
        }
    }

    private static void ValidateText(
        string value,
        int maximumLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Trim().Length > maximumLength)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
