using Ekiphan.Application.Catalog;
using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.Media;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Catalog;

internal sealed class AdminProductManagementService(
    EkiphanDbContext dbContext)
    : IAdminProductManagementService
{
    public Task<AdminProductDetail?> GetAsync(
        Guid productId,
        CancellationToken cancellationToken = default) =>
        ProjectProducts(productId)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<AdminProductDetail> CreateAsync(
        SaveAdminProductCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        await EnsureBrandExistsAsync(command.BrandId, cancellationToken);
        await EnsureCategoriesExistAsync(command.CategoryIds, cancellationToken);
        await EnsureTagsExistAsync(command.TagIds, cancellationToken);
        await EnsureSeoMediaExistsAsync(command.Translations, cancellationToken);
        var product = new Product(Guid.NewGuid(), command.SKU, command.BrandId);
        Apply(product, command);
        dbContext.Products.Add(product);
        await ApplyAttributeValuesAsync(
            product.Id,
            command,
            cancellationToken);
        await SaveAsync(cancellationToken);
        return await GetRequiredAsync(product.Id, cancellationToken);
    }

    public async Task<AdminProductDetail?> UpdateAsync(
        Guid productId,
        SaveAdminProductCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        await EnsureBrandExistsAsync(command.BrandId, cancellationToken);
        await EnsureCategoriesExistAsync(command.CategoryIds, cancellationToken);
        await EnsureTagsExistAsync(command.TagIds, cancellationToken);
        await EnsureSeoMediaExistsAsync(command.Translations, cancellationToken);
        var product = await dbContext.Products
            .Include(item => item.Translations)
            .Include(item => item.Categories)
            .Include(item => item.Tags)
            .SingleOrDefaultAsync(
                item => item.Id == productId,
                cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.UpdateIdentity(command.SKU, command.BrandId);
        Apply(product, command);
        await ApplyAttributeValuesAsync(
            product.Id,
            command,
            cancellationToken);
        await SaveAsync(cancellationToken);
        return await GetRequiredAsync(product.Id, cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(
            item => item.Id == productId,
            cancellationToken);
        if (product is null)
        {
            return false;
        }

        product.SoftDelete(DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<AdminProductDetail> ProjectProducts(Guid productId) =>
        dbContext.Products
            .AsNoTracking()
            .Where(product => product.Id == productId)
            .Select(product => new AdminProductDetail(
                product.Id,
                product.SKU,
                product.BrandId,
                product.IsPublished,
                product.Translations
                    .OrderBy(translation => translation.LanguageCode)
                    .Select(translation => new AdminProductTranslation(
                        translation.LanguageCode,
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
                        translation.OpenGraphImageMediaId,
                        null))
                    .ToArray(),
                product.CreatedAt,
                product.UpdatedAt,
                product.Categories
                    .OrderBy(item => item.SortOrder)
                    .Select(item => item.CategoryId)
                    .ToArray(),
                product.PrimaryCategoryId,
                dbContext.ProductAttributeValues
                    .Where(value => value.ProductId == product.Id)
                    .OrderBy(value => value.AttributeId)
                    .ThenBy(value => value.Sequence)
                    .Select(value => new AdminProductAttributeValueInput(
                        value.AttributeId,
                        value.Sequence,
                        value.TextValue,
                        value.NumericValue,
                        value.BooleanValue,
                        value.AttributeOptionId,
                        value.UnitId))
                    .ToArray(),
                product.Tags
                    .OrderBy(item => item.SortOrder)
                    .Select(item => item.TagId)
                    .ToArray()));

    private async Task<AdminProductDetail> GetRequiredAsync(
        Guid productId,
        CancellationToken cancellationToken) =>
        await GetAsync(productId, cancellationToken) ??
        throw new InvalidOperationException(
            "The saved product could not be reloaded.");

    private async Task EnsureBrandExistsAsync(
        Guid? brandId,
        CancellationToken cancellationToken)
    {
        if (brandId.HasValue &&
            !await dbContext.Brands.AnyAsync(
                brand => brand.Id == brandId.Value,
                cancellationToken))
        {
            throw new ArgumentException(
                "The selected brand does not exist.",
                nameof(brandId));
        }
    }

    private async Task EnsureSeoMediaExistsAsync(
        IReadOnlyList<AdminProductTranslationInput> translations,
        CancellationToken cancellationToken)
    {
        var ids = translations.Where(item => item.OpenGraphImageMediaId.HasValue)
            .Select(item => item.OpenGraphImageMediaId!.Value).Distinct().ToArray();
        if (ids.Length == 0) return;
        var count = await dbContext.MediaAssets.CountAsync(item =>
            ids.Contains(item.Id) && item.Status == MediaStatus.Active &&
            item.AssetType == MediaAssetType.Image, cancellationToken);
        if (count != ids.Length)
            throw new ArgumentException("Open Graph images must reference active image media.");
    }

    private async Task EnsureCategoriesExistAsync(
        IReadOnlyList<Guid>? categoryIds,
        CancellationToken cancellationToken)
    {
        if (categoryIds is null || categoryIds.Count == 0)
        {
            return;
        }

        var distinctIds = categoryIds.Distinct().ToArray();
        var foundCount = await dbContext.Categories.CountAsync(
            item => distinctIds.Contains(item.Id),
            cancellationToken);
        if (foundCount != distinctIds.Length)
        {
            throw new ArgumentException(
                "One or more selected categories do not exist.",
                nameof(categoryIds));
        }
    }

    private async Task EnsureTagsExistAsync(
        IReadOnlyList<Guid>? tagIds,
        CancellationToken cancellationToken)
    {
        if (tagIds is null || tagIds.Count == 0)
        {
            return;
        }

        var distinctIds = tagIds.Distinct().ToArray();
        var foundCount = await dbContext.Tags.CountAsync(
            item => distinctIds.Contains(item.Id) && item.IsActive,
            cancellationToken);
        if (foundCount != distinctIds.Length)
        {
            throw new ArgumentException(
                "One or more selected tags do not exist or are inactive.",
                nameof(tagIds));
        }
    }

    private async Task ApplyAttributeValuesAsync(
        Guid productId,
        SaveAdminProductCommand command,
        CancellationToken cancellationToken)
    {
        var values = command.AttributeValues ?? [];
        var categoryIds = command.CategoryIds ?? [];
        List<CategoryAttributeAssignment> assignments = categoryIds.Count == 0
            ? []
            : await dbContext.CategoryAttributes
                .AsNoTracking()
                .Where(item => categoryIds.Contains(item.CategoryId))
                .ToListAsync(cancellationToken);
        var allowedIds = assignments
            .Select(item => item.AttributeId)
            .ToHashSet();
        var valueAttributeIds = values
            .Select(item => item.AttributeId)
            .ToHashSet();
        if (values.Any(item => !allowedIds.Contains(item.AttributeId)))
        {
            throw new ArgumentException(
                "A product value references an attribute outside its categories.");
        }

        var duplicateSequence = values
            .GroupBy(item => new { item.AttributeId, item.Sequence })
            .Any(group => group.Count() > 1);
        if (duplicateSequence || values.Any(item => item.Sequence < 0))
        {
            throw new ArgumentException(
                "Attribute value sequences must be non-negative and unique.");
        }

        if (values
            .Where(item => item.AttributeOptionId.HasValue)
            .GroupBy(item => new { item.AttributeId, item.AttributeOptionId })
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "The same option cannot be selected more than once.");
        }

        var definitions = await dbContext.Attributes
            .AsNoTracking()
            .Where(item => valueAttributeIds.Contains(item.Id))
            .Select(item => new
            {
                item.Id,
                item.DataType,
                item.UnitDimension,
                item.IsActive,
                OptionIds = item.Options
                    .Where(option => option.IsActive)
                    .Select(option => option.Id)
                    .ToArray(),
            })
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var requiredIds = assignments
            .Where(item => item.IsRequired)
            .Select(item => item.AttributeId)
            .Where(item => definitions.TryGetValue(item, out var definition) &&
                definition.IsActive)
            .ToHashSet();
        if (!requiredIds.IsSubsetOf(valueAttributeIds))
        {
            throw new ArgumentException(
                "All required category attributes must have a value.");
        }
        var unitIds = values
            .Where(item => item.UnitId.HasValue)
            .Select(item => item.UnitId!.Value)
            .Distinct()
            .ToArray();
        var units = await dbContext.Units
            .AsNoTracking()
            .Where(item => unitIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        var existing = await dbContext.ProductAttributeValues
            .Where(item => item.ProductId == productId)
            .ToListAsync(cancellationToken);
        dbContext.ProductAttributeValues.RemoveRange(existing);
        foreach (var value in values)
        {
            if (!definitions.TryGetValue(value.AttributeId, out var definition) ||
                !definition.IsActive)
            {
                throw new ArgumentException(
                    "The selected attribute does not exist.");
            }

            ProductAttributeValue entity = definition.DataType switch
            {
                AttributeDataType.Text when
                    value.TextValue is not null &&
                    Only(value, "text") =>
                    ProductAttributeValue.FromText(
                        Guid.NewGuid(),
                        productId,
                        value.AttributeId,
                        value.TextValue,
                        value.Sequence),
                AttributeDataType.Number when
                    value.NumericValue.HasValue &&
                    Only(value, "number") &&
                    ValidUnit(definition.UnitDimension, value.UnitId, units) =>
                    ProductAttributeValue.FromNumber(
                        Guid.NewGuid(),
                        productId,
                        value.AttributeId,
                        value.NumericValue.Value,
                        value.UnitId,
                        value.Sequence),
                AttributeDataType.Boolean when
                    value.BooleanValue.HasValue &&
                    Only(value, "boolean") =>
                    ProductAttributeValue.FromBoolean(
                        Guid.NewGuid(),
                        productId,
                        value.AttributeId,
                        value.BooleanValue.Value,
                        value.Sequence),
                AttributeDataType.Option when
                    value.AttributeOptionId.HasValue &&
                    value.Sequence == 0 &&
                    Only(value, "option") &&
                    definition.OptionIds.Contains(value.AttributeOptionId.Value) =>
                    ProductAttributeValue.FromOption(
                        Guid.NewGuid(),
                        productId,
                        value.AttributeId,
                        value.AttributeOptionId.Value,
                        value.Sequence),
                AttributeDataType.MultiOption when
                    value.AttributeOptionId.HasValue &&
                    Only(value, "option") &&
                    definition.OptionIds.Contains(value.AttributeOptionId.Value) =>
                    ProductAttributeValue.FromOption(
                        Guid.NewGuid(),
                        productId,
                        value.AttributeId,
                        value.AttributeOptionId.Value,
                        value.Sequence),
                _ => throw new ArgumentException(
                    "An attribute value does not match its declared data type."),
            };
            dbContext.ProductAttributeValues.Add(entity);
        }
    }

    private static bool Only(
        AdminProductAttributeValueInput value,
        string selected) =>
        (selected == "text" ? 1 : 0) == (value.TextValue is null ? 0 : 1) &&
        (selected == "number" ? 1 : 0) == (value.NumericValue.HasValue ? 1 : 0) &&
        (selected == "boolean" ? 1 : 0) == (value.BooleanValue.HasValue ? 1 : 0) &&
        (selected == "option" ? 1 : 0) == (value.AttributeOptionId.HasValue ? 1 : 0);

    private static bool ValidUnit(
        string? dimension,
        Guid? unitId,
        Dictionary<Guid, UnitDefinition> units)
    {
        if (!unitId.HasValue)
        {
            return dimension is null;
        }

        return dimension is not null &&
            units.TryGetValue(unitId.Value, out var unit) &&
            unit.IsActive &&
            unit.Dimension == dimension;
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new AdminProductConflictException(
                "The SKU or translated slug is already in use.");
        }
    }

    private static void Apply(
        Product product,
        SaveAdminProductCommand command)
    {
        var requestedLanguages = command.Translations
            .Select(translation => translation.LanguageCode.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
        foreach (var translation in command.Translations)
        {
            product.SetTranslation(
                translation.LanguageCode,
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
                translation.OpenGraphImageMediaId);
        }

        foreach (var language in product.Translations
            .Select(translation => translation.LanguageCode)
            .Where(language => !requestedLanguages.Contains(language))
            .ToArray())
        {
            product.RemoveTranslation(language);
        }

        product.SetPublished(command.IsPublished);
        product.SetCategories(
            command.CategoryIds ?? [],
            command.PrimaryCategoryId);
        product.SetTags(command.TagIds ?? []);
    }

    private static void Validate(SaveAdminProductCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Translations is null ||
            command.Translations.Count is < 1 or > 2)
        {
            throw new ArgumentException(
                "One or two translations are required.",
                nameof(command));
        }

        var languages = command.Translations
            .Select(translation => translation.LanguageCode?.ToLowerInvariant())
            .ToArray();
        if (languages.Any(language => language is not ("tr" or "en")) ||
            languages.Distinct(StringComparer.Ordinal).Count() !=
            languages.Length)
        {
            throw new ArgumentException(
                "Translations must use unique tr or en language codes.",
                nameof(command));
        }

        var categoryIds = command.CategoryIds ?? [];
        if (categoryIds.Any(item => item == Guid.Empty) ||
            categoryIds.Distinct().Count() != categoryIds.Count ||
            command.PrimaryCategoryId.HasValue &&
            !categoryIds.Contains(command.PrimaryCategoryId.Value))
        {
            throw new ArgumentException(
                "Categories must be unique and include the primary category.",
                nameof(command));
        }

        var tagIds = command.TagIds ?? [];
        if (tagIds.Any(item => item == Guid.Empty) ||
            tagIds.Distinct().Count() != tagIds.Count)
        {
            throw new ArgumentException(
                "Tags must be non-empty and unique.",
                nameof(command));
        }
    }
}
