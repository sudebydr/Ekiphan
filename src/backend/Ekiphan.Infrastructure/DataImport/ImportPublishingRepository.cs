using Ekiphan.Application.DataImport;
using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.Common;
using Ekiphan.Domain.DataImport;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.DataImport;

internal sealed class ImportPublishingRepository(EkiphanDbContext dbContext)
    : IImportPublishingRepository
{
    private const int SkuQueryBatchSize = 1_000;
    private readonly Dictionary<string, AttributeDefinition> _preparedDefinitions = new(StringComparer.Ordinal);
    private readonly Dictionary<Guid, List<ProductAttributeValue>> _preparedValues = [];
    private readonly HashSet<Guid> _preparedProductIds = [];
    public Task<ImportJob?> GetJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        dbContext.ImportJobs
            .Include(job => job.Rows)
            .SingleOrDefaultAsync(job => job.Id == jobId, cancellationToken);

    public async Task<HashSet<string>> GetExistingSkusAsync(
        IReadOnlyCollection<string> skus,
        CancellationToken cancellationToken = default)
    {
        var existingSkus = new HashSet<string>(StringComparer.Ordinal);
        foreach (var batch in skus
            .Select(SkuNormalizer.Normalize).Distinct(StringComparer.Ordinal)
            .Chunk(SkuQueryBatchSize))
        {
            var matches = await dbContext.Products
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(product => batch.Contains(product.SKU))
                .Select(product => product.SKU)
                .ToListAsync(cancellationToken);
            existingSkus.UnionWith(matches.Select(SkuNormalizer.Normalize));
        }

        return existingSkus;
    }

    public async Task<IReadOnlyDictionary<string, Product>> GetProductsBySkusAsync(
        IReadOnlyCollection<string> skus,
        CancellationToken cancellationToken = default)
    {
        var products = new List<Product>();
        foreach (var batch in skus.Select(SkuNormalizer.Normalize).Distinct(StringComparer.Ordinal).Chunk(SkuQueryBatchSize))
        {
            products.AddRange(await dbContext.Products.IgnoreQueryFilters()
                .Include(product => product.Translations)
                .Include(product => product.Categories)
                .Include(product => product.Tags)
                .Include(product => product.VariantGroups).ThenInclude(group => group.Translations)
                .Include(product => product.VariantGroups).ThenInclude(group => group.Options).ThenInclude(option => option.Translations)
                .Include(product => product.Variants).ThenInclude(variant => variant.Selections)
                .Where(product => batch.Contains(product.SKU))
                .ToListAsync(cancellationToken));
        }
        return products.ToDictionary(product => SkuNormalizer.Normalize(product.SKU), StringComparer.Ordinal);
    }

    public void AddProduct(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        dbContext.Products.Add(product);
    }

    public void AddAttributeValue(ProductAttributeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        dbContext.ProductAttributeValues.Add(value);
    }

    public void AddIssue(ImportIssue issue)
    {
        ArgumentNullException.ThrowIfNull(issue);
        dbContext.Set<ImportIssue>().Add(issue);
    }

    public async Task PrepareProductDataAsync(
        IReadOnlyCollection<Guid> productIds,
        IReadOnlyCollection<string> attributeCodes,
        IReadOnlyCollection<Guid> materialAttributeIds,
        CancellationToken cancellationToken = default)
    {
        var importCodes = attributeCodes.Select(code => "IMPORT_" + code)
            .Distinct(StringComparer.Ordinal).ToArray();
        var definitions = dbContext.Attributes.Local
            .Where(attribute => importCodes.Contains(attribute.Code))
            .ToArray();
        foreach (var definition in definitions)
            _preparedDefinitions[definition.Code] = definition;
        foreach (var batch in importCodes.Chunk(SkuQueryBatchSize))
        {
            var persisted = await dbContext.Attributes
                .Include(attribute => attribute.Translations)
                .Where(attribute => batch.Contains(attribute.Code))
                .ToListAsync(cancellationToken);
            foreach (var definition in persisted)
                _preparedDefinitions[definition.Code] = definition;
        }

        var attributeIds = _preparedDefinitions.Values.Select(definition => definition.Id)
            .Concat(materialAttributeIds).Distinct().ToArray();
        if (productIds.Count == 0 || attributeIds.Length == 0)
            return;
        foreach (var batch in productIds.Distinct().Chunk(SkuQueryBatchSize))
        {
            var values = await dbContext.ProductAttributeValues
                .Where(value => batch.Contains(value.ProductId) && attributeIds.Contains(value.AttributeId))
                .ToListAsync(cancellationToken);
            foreach (var productId in batch)
                _preparedValues.TryAdd(productId, []);
            foreach (var value in values)
                _preparedValues[value.ProductId].Add(value);
            _preparedProductIds.UnionWith(batch);
        }
    }

    public async Task ReplaceImportedAttributesAsync(Guid productId,
        IReadOnlyDictionary<string, string[]> values,
        CancellationToken cancellationToken = default)
    {
        var codes = values.Keys.Select(code => "IMPORT_" + code)
            .Distinct(StringComparer.Ordinal).ToArray();
        var definitions = new Dictionary<string, AttributeDefinition>(_preparedDefinitions, StringComparer.Ordinal);
        if (!_preparedProductIds.Contains(productId))
        {
            foreach (var persistedDefinition in await dbContext.Attributes.Include(attribute => attribute.Translations)
                         .Where(attribute => codes.Contains(attribute.Code)).ToListAsync(cancellationToken))
                definitions.TryAdd(persistedDefinition.Code, persistedDefinition);
        }
        foreach (var item in values)
        {
            var code = "IMPORT_" + item.Key;
            if (!definitions.TryGetValue(code, out var definition))
            {
                definition = new AttributeDefinition(Guid.NewGuid(), code, AttributeDataType.Text);
                definition.AddTranslation("tr", AttributeTitle(item.Key));
                dbContext.Attributes.Add(definition); definitions[code] = definition;
                _preparedDefinitions[code] = definition;
            }
        }
        var ids = definitions.Values.Select(item => item.Id).ToArray();
        var oldValues = _preparedProductIds.Contains(productId)
            ? _preparedValues.GetValueOrDefault(productId, []).Where(value => ids.Contains(value.AttributeId)).ToList()
            : await dbContext.ProductAttributeValues.Where(value => value.ProductId == productId && ids.Contains(value.AttributeId)).ToListAsync(cancellationToken);
        dbContext.ProductAttributeValues.RemoveRange(oldValues);
        if (_preparedValues.TryGetValue(productId, out var preparedValues))
            preparedValues.RemoveAll(value => ids.Contains(value.AttributeId));
        foreach (var item in values)
        {
            var definition = definitions["IMPORT_" + item.Key];
            for (var index = 0; index < item.Value.Length; index++)
            {
                var value = ProductAttributeValue.FromText(Guid.NewGuid(), productId,
                    definition.Id, item.Value[index], index, item.Value[index]);
                dbContext.ProductAttributeValues.Add(value);
                if (_preparedValues.TryGetValue(productId, out var valuesForProduct)) valuesForProduct.Add(value);
            }
        }
    }

    public async Task ReplaceMaterialAsync(Guid productId, Guid? attributeId, Guid? optionId, string? rawValue,
        CancellationToken cancellationToken = default)
    {
        if (!attributeId.HasValue) return;
        var existing = _preparedProductIds.Contains(productId)
            ? _preparedValues.GetValueOrDefault(productId, []).Where(value => value.AttributeId == attributeId.Value).ToList()
            : await dbContext.ProductAttributeValues.Where(value => value.ProductId == productId && value.AttributeId == attributeId.Value).ToListAsync(cancellationToken);
        dbContext.ProductAttributeValues.RemoveRange(existing);
        if (_preparedValues.TryGetValue(productId, out var preparedValues))
            preparedValues.RemoveAll(value => value.AttributeId == attributeId.Value);
        if (optionId.HasValue)
        {
            var value = ProductAttributeValue.FromOption(Guid.NewGuid(), productId,
                attributeId.Value, optionId.Value, rawValue: rawValue);
            dbContext.ProductAttributeValues.Add(value);
            if (_preparedValues.TryGetValue(productId, out var valuesForProduct)) valuesForProduct.Add(value);
        }
    }

    public Task UpsertVariantAsync(Product product, string variantKey, int sortOrder,
        CancellationToken cancellationToken = default)
    {
        const string groupCode = "IMPORT_VARIANT";
        var group = product.VariantGroups.FirstOrDefault(item => item.Code == groupCode);
        if (group is null)
        {
            group = product.AddVariantGroup(Guid.NewGuid(), groupCode, 0);
            group.AddTranslation("tr", "Varyant");
        }

        var hash = ProductImportNormalizer.VariantKeyHash(variantKey);
        var optionCode = $"V_{hash}";
        var option = group.Options.FirstOrDefault(item => item.Code == optionCode);
        if (option is null)
        {
            option = group.AddOption(Guid.NewGuid(), optionCode, group.Options.Count);
            option.AddTranslation("tr", variantKey.Length <= 200 ? variantKey : variantKey[..200]);
        }
        option.SetActive(true);

        var selected = new Dictionary<Guid, Guid> { [group.Id] = option.Id };
        if (product.Variants.Any(variant => variant.Selections.Count == 1 &&
            variant.Selections.Any(selection => selection.VariantGroupId == group.Id && selection.VariantOptionId == option.Id)))
            return Task.CompletedTask;

        var prefixLength = Math.Min(product.SKU.Length, 82);
        product.AddVariant(Guid.NewGuid(), $"{product.SKU[..prefixLength]}-{hash}", selected, sortOrder);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<string>> ApplyRelationsAsync(Guid sourceProductId,
        IReadOnlyCollection<string> similarSkus, IReadOnlyCollection<string> complementarySkus,
        CancellationToken cancellationToken = default)
    {
        var requested = similarSkus.Concat(complementarySkus).Select(SkuNormalizer.Normalize).Distinct(StringComparer.Ordinal).ToArray();
        var targets = await dbContext.Products.IgnoreQueryFilters().Where(product => requested.Contains(product.SKU) && !product.IsDeleted)
            .ToDictionaryAsync(product => SkuNormalizer.Normalize(product.SKU), StringComparer.Ordinal, cancellationToken);
        var missing = requested.Where(sku => !targets.ContainsKey(sku)).ToArray();
        async Task Add(IReadOnlyCollection<string> skus, ProductRelationType type)
        {
            var targetIds = skus.Select(SkuNormalizer.Normalize).Where(targets.ContainsKey).Select(sku => targets[sku].Id)
                .Where(id => id != sourceProductId).Distinct().ToArray();
            var existing = dbContext.ChangeTracker.Entries<ProductRelation>()
                .Where(entry => entry.State != EntityState.Deleted &&
                    entry.Entity.SourceProductId == sourceProductId &&
                    entry.Entity.RelationType == type && targetIds.Contains(entry.Entity.TargetProductId))
                .Select(entry => entry.Entity.TargetProductId)
                .ToHashSet();
            var persisted = await dbContext.ProductRelations
                .Where(relation => relation.SourceProductId == sourceProductId &&
                    relation.RelationType == type && targetIds.Contains(relation.TargetProductId))
                .Select(relation => relation.TargetProductId)
                .ToListAsync(cancellationToken);
            existing.UnionWith(persisted);
            var order = 0;
            foreach (var id in targetIds.Where(id => existing.Add(id)))
                dbContext.ProductRelations.Add(new ProductRelation(Guid.NewGuid(), sourceProductId, id, type, false, order++));
        }
        await Add(similarSkus, ProductRelationType.Similar);
        await Add(complementarySkus, ProductRelationType.Complementary);
        return missing;
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> ApplyRelationsBatchAsync(
        IReadOnlyCollection<ImportRelationRequest> requests,
        CancellationToken cancellationToken = default)
    {
        if (requests.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<string>>();

        var requestedSkus = requests.SelectMany(request => request.SimilarSkus.Concat(request.ComplementarySkus))
            .Select(SkuNormalizer.Normalize).Distinct(StringComparer.Ordinal).ToArray();
        var targets = new Dictionary<string, Product>(StringComparer.Ordinal);
        foreach (var batch in requestedSkus.Chunk(SkuQueryBatchSize))
        {
            var products = await dbContext.Products.IgnoreQueryFilters()
                .Where(product => batch.Contains(product.SKU) && !product.IsDeleted)
                .ToListAsync(cancellationToken);
            foreach (var product in products)
                targets[SkuNormalizer.Normalize(product.SKU)] = product;
        }

        var sourceIds = requests.Select(request => request.SourceProductId).Distinct().ToArray();
        var targetIds = targets.Values.Select(product => product.Id).Distinct().ToArray();
        var relationKeys = dbContext.ChangeTracker.Entries<ProductRelation>()
            .Where(entry => entry.State != EntityState.Deleted)
            .Select(entry => (entry.Entity.SourceProductId, entry.Entity.TargetProductId, entry.Entity.RelationType))
            .ToHashSet();
        if (targetIds.Length > 0)
        {
            foreach (var batch in sourceIds.Chunk(SkuQueryBatchSize))
            {
                var persisted = await dbContext.ProductRelations
                    .Where(relation => batch.Contains(relation.SourceProductId) && targetIds.Contains(relation.TargetProductId))
                    .Select(relation => new { relation.SourceProductId, relation.TargetProductId, relation.RelationType })
                    .ToListAsync(cancellationToken);
                foreach (var relation in persisted)
                    relationKeys.Add((relation.SourceProductId, relation.TargetProductId, relation.RelationType));
            }
        }

        var missingByRow = new Dictionary<Guid, IReadOnlyList<string>>();
        var sortOrders = new Dictionary<(Guid SourceId, ProductRelationType Type), int>();
        foreach (var request in requests)
        {
            var requested = request.SimilarSkus.Concat(request.ComplementarySkus)
                .Select(SkuNormalizer.Normalize).Distinct(StringComparer.Ordinal).ToArray();
            missingByRow[request.RowId] = requested.Where(sku => !targets.ContainsKey(sku)).ToArray();
            Add(request.SimilarSkus, ProductRelationType.Similar);
            Add(request.ComplementarySkus, ProductRelationType.Complementary);

            void Add(IReadOnlyCollection<string> skus, ProductRelationType type)
            {
                foreach (var targetId in skus.Select(SkuNormalizer.Normalize).Where(targets.ContainsKey).Select(sku => targets[sku].Id)
                             .Where(id => id != request.SourceProductId).Distinct())
                {
                    var key = (request.SourceProductId, targetId, type);
                    if (!relationKeys.Add(key)) continue;
                    var orderKey = (request.SourceProductId, type);
                    var sortOrder = sortOrders.GetValueOrDefault(orderKey);
                    sortOrders[orderKey] = sortOrder + 1;
                    dbContext.ProductRelations.Add(new ProductRelation(Guid.NewGuid(), request.SourceProductId,
                        targetId, type, false, sortOrder));
                }
            }
        }
        return missingByRow;
    }

    private static string AttributeTitle(string code) => code switch
    {
        "PACKAGE_QUANTITY" => "Koli İçi Adet", "COLOR" => "Renk", "MODEL_SERIES" => "Model/Seri",
        "USAGE_AREA" => "Kullanım Alanı", "SHAPE" => "Şekil", "VOLUME_CC" => "Hacim (cc)",
        "VOLUME_LT" => "Hacim (lt)", "DIAMETER_CM" => "Çap (cm)", "DIAMETER_MM" => "Çap (mm)",
        "WIDTH_CM" => "En (cm)", "WIDTH_MM" => "En (mm)", "LENGTH_CM" => "Boy (cm)",
        "LENGTH_MM" => "Boy (mm)", "HEIGHT_CM" => "Yükseklik (cm)", "HEIGHT_MM" => "Yükseklik (mm)",
        "WEIGHT_GR" => "Ağırlık (gr)", "MAIN_IMAGE_FILE" => "Ana Görsel Dosyası",
        "ADDITIONAL_IMAGE_FILE" => "İlave Görsel Dosyası", "TECHNICAL_DRAWING_FILE" => "Teknik Çizim Dosyası",
        "CATALOG_FILE_OR_URL" => "Katalog Dosyası/Bağlantısı", "SEARCH_SYNONYMS" => "Arama Eş Anlamlıları",
        "VARIANT" => "Varyant", "SORT_PRIORITY" => "Sıralama/Öncelik", "PRICE_SEGMENT" => "Fiyat Segmenti",
        _ => code
    };

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(
            async () =>
            {
                await using var transaction =
                    await dbContext.Database.BeginTransactionAsync(
                        cancellationToken);
                await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            });
    }
}
