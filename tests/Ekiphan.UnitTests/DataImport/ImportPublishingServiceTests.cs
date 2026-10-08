using Ekiphan.Application.DataImport;
using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.DataImport;

namespace Ekiphan.UnitTests.DataImport;

public sealed class ImportPublishingServiceTests
{
    [Fact]
    public async Task PublishAsyncCreatesAndUpdatesProductsBySku()
    {
        var job = CreateReadyJob(("NEW-1", "Yeni Ürün"), ("OLD-1", "Eski Ürün"));
        var repository = new StubRepository(job, ["OLD-1"]);
        var service = new ImportPublishingService(
            repository,
            TimeProvider.System);

        var result = await service.PublishAsync(job.Id);

        Assert.Equal(ImportJobStatus.Completed, result.Status);
        Assert.Equal(2, result.PublishedRowCount);
        Assert.Equal(0, result.SkippedRowCount);
        var product = repository.Products.Single(item => item.SKU == "NEW-1");
        Assert.Equal("NEW-1", product.SKU);
        Assert.True(product.IsPublished);
        Assert.Equal(job.Id, product.CreatedByImportJobId);
        Assert.Null(repository.Products.Single(item => item.SKU == "OLD-1").CreatedByImportJobId);
        var translation = Assert.Single(product.Translations);
        Assert.Equal("tr", translation.LanguageCode);
        Assert.Equal("Yeni Ürün", translation.Name);
        Assert.EndsWith(product.Id.ToString("N"), translation.Slug);
        Assert.True(repository.TransactionExecuted);
        Assert.Equal("Eski Ürün", repository.Products.Single(item => item.SKU == "OLD-1")
            .Translations.Single().Name);
    }

    [Fact]
    public async Task PublishAsyncRejectsMissingJob()
    {
        var repository = new StubRepository(null, []);
        var service = new ImportPublishingService(
            repository,
            TimeProvider.System);

        await Assert.ThrowsAsync<ImportJobNotFoundException>(
            () => service.PublishAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task PublishAsyncRejectsDryRunJob()
    {
        var job = CreateReadyJob(isDryRun: true, ("DRY-1", "Dry Run"));
        var repository = new StubRepository(job, []);
        var service = new ImportPublishingService(
            repository,
            TimeProvider.System);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PublishAsync(job.Id));

        Assert.Empty(repository.Products);
    }

    [Fact]
    public async Task PublishAsyncAssignsOnlyResolvedBrandAndCategories()
    {
        var brandId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var materialAttributeId = Guid.NewGuid();
        var materialOptionId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var job = new ImportJob(
            Guid.NewGuid(),
            ImportSourceType.Csv,
            "products.csv",
            new string('C', 64),
            isDryRun: false);
        job.StartValidation(DateTimeOffset.UtcNow);
        var row = job.AddRow(
            Guid.NewGuid(),
            "Products",
            2,
            """{"SKU":"REF-1","Name":"Referanslı"}""",
            "REF-1");
        row.MarkValid(
            $$"""{"sku":"REF-1","name":"Referanslı","brandId":"{{brandId}}","categoryIds":["{{categoryId}}"],"tagIds":["{{tagId}}"],"material":"Çelik","materialAttributeId":"{{materialAttributeId}}","materialOptionId":"{{materialOptionId}}"}""");
        job.CompleteValidation(DateTimeOffset.UtcNow);
        var repository = new StubRepository(job, []);
        var service = new ImportPublishingService(
            repository,
            TimeProvider.System);

        await service.PublishAsync(job.Id);

        var product = Assert.Single(repository.Products);
        Assert.Equal(brandId, product.BrandId);
        Assert.Equal(categoryId, Assert.Single(product.Categories).CategoryId);
        Assert.Equal(categoryId, product.PrimaryCategoryId);
        Assert.Equal(tagId, Assert.Single(product.Tags).TagId);
        var materialValue = Assert.Single(repository.AttributeValues);
        Assert.Equal(product.Id, materialValue.ProductId);
        Assert.Equal(materialAttributeId, materialValue.AttributeId);
        Assert.Equal(materialOptionId, materialValue.AttributeOptionId);
        Assert.Equal("Çelik", materialValue.RawValue);
    }

    [Fact]
    public async Task PublishAsyncDoesNotRepublishExistingUnpublishedProduct()
    {
        var job = CreateReadyJob(("OLD-1", "Güncel Eski Ürün"));
        var repository = new StubRepository(job, ["OLD-1"]);
        var existingProduct = Assert.Single(repository.Products);
        existingProduct.SetPublished(true);
        existingProduct.SetPublished(false);

        await new ImportPublishingService(repository, TimeProvider.System)
            .PublishAsync(job.Id);

        Assert.False(existingProduct.IsPublished);
        Assert.Equal(ProductWorkflowStatus.Unpublished, existingProduct.WorkflowStatus);
        Assert.Equal("Güncel Eski Ürün", existingProduct.Translations.Single().Name);
    }

    [Fact]
    public async Task PublishAsyncPersistsAttributesAndResolvesRelationsAfterAllRows()
    {
        var job = new ImportJob(Guid.NewGuid(), ImportSourceType.Excel, "products.xlsx", new string('D', 64), false);
        job.StartValidation(DateTimeOffset.UtcNow);
        var source = job.AddRow(Guid.NewGuid(), "Products", 2, "{}", "SOURCE");
        source.MarkValid("""{"sku":"SOURCE","name":"Source","attributeValues":{"COLOR":["Kırmızı"],"MODEL_SERIES":["M1"],"WEIGHT_GR":["700"],"USAGE_AREA":["Otel"]},"similarSkus":["LATER"],"complementarySkus":["MISSING"]}""");
        var later = job.AddRow(Guid.NewGuid(), "Products", 3, "{}", "LATER");
        later.MarkValid("""{"sku":"LATER","name":"Later"}""");
        job.CompleteValidation(DateTimeOffset.UtcNow);
        var repository = new StubRepository(job, []);

        await new ImportPublishingService(repository, TimeProvider.System).PublishAsync(job.Id);

        var sourceId = repository.Products.Single(product => product.SKU == "SOURCE").Id;
        var saved = Assert.Single(repository.ImportedAttributes, item => item.ProductId == sourceId);
        Assert.Equal(["Kırmızı"], saved.Values["COLOR"]);
        Assert.Equal(["M1"], saved.Values["MODEL_SERIES"]);
        Assert.Contains(repository.Relations, relation => relation.Similar.SequenceEqual(["LATER"]));
        Assert.Contains(repository.Relations, relation => relation.Complementary.SequenceEqual(["MISSING"]));
        Assert.DoesNotContain(source.Issues, issue => issue.Code == "RELATED_SKU_NOT_FOUND");
    }

    [Fact]
    public async Task PublishAsyncUsesLowestRowNumberAsCanonicalProductDataAndWarnsOnDifference()
    {
        var job = CreateReadyJobWithRows(
            (2, "ABC123", """{"sku":"ABC123","name":"Master","variantKey":"COLOR=RED"}"""),
            (3, "ABC123", """{"sku":"ABC123","name":"Other","variantKey":"COLOR=BLUE"}"""));
        var repository = new StubRepository(job, []);

        await new ImportPublishingService(repository, TimeProvider.System).PublishAsync(job.Id);

        Assert.Equal("Master", Assert.Single(repository.Products).Translations.Single().Name);
        var warning = Assert.Single(job.Rows.Single(row => row.RowNumber == 3).Issues,
            issue => issue.Code == "SAME_SKU_PRODUCT_DATA_DIFFER");
        Assert.Equal(ImportIssueSeverity.Warning, warning.Severity);
        Assert.Contains("ABC123", warning.Message);
        Assert.Contains("2", warning.Message);
        Assert.Contains("3", warning.Message);
        Assert.Contains("name", warning.Message);
    }

    [Fact]
    public async Task PublishAsyncKeepsDifferentVariantsWithoutProductLevelWarning()
    {
        var job = CreateReadyJobWithRows(
            (2, "ABC123", """{"sku":"ABC123","name":"Product","variantKey":"COLOR=RED"}"""),
            (3, "ABC123", """{"sku":"ABC123","name":"Product","variantKey":"COLOR=BLUE"}"""));
        var repository = new StubRepository(job, []);

        await new ImportPublishingService(repository, TimeProvider.System).PublishAsync(job.Id);

        Assert.DoesNotContain(job.Rows.SelectMany(row => row.Issues),
            issue => issue.Code == "SAME_SKU_PRODUCT_DATA_DIFFER");
        Assert.Equal(["COLOR=RED", "COLOR=BLUE"], repository.VariantKeys.Select(item => item.VariantKey));
    }

    [Fact]
    public async Task PublishAsyncUsesRowNumberWhenJobRowsAreStoredInReverseOrder()
    {
        var job = CreateReadyJobWithRows(
            (3, "ABC123", """{"sku":"ABC123","name":"Later"}"""),
            (2, "ABC123", """{"sku":"ABC123","name":"First"}"""));
        var repository = new StubRepository(job, []);

        await new ImportPublishingService(repository, TimeProvider.System).PublishAsync(job.Id);

        Assert.Equal("First", Assert.Single(repository.Products).Translations.Single().Name);
    }

    [Fact]
    public async Task PublishAsyncTruncatesOversizedMetaTitleWithoutChangingOtherProductData()
    {
        var metaTitle = new string('M', 71);
        var job = CreateReadyJobWithRows(
            (2, "META-1", $$"""{"sku":"META-1","name":"Product","metaTitle":"{{metaTitle}}"}"""));
        var repository = new StubRepository(job, []);

        await new ImportPublishingService(repository, TimeProvider.System).PublishAsync(job.Id);

        var translation = Assert.Single(Assert.Single(repository.Products).Translations);
        Assert.Equal(70, translation.MetaTitle!.Length);
        Assert.Equal("Product", translation.Name);
    }

    [Fact]
    public async Task PublishAsyncUsesCanonicalSkuForRawRowAndNormalizedPayloadDuringRelationLookup()
    {
        var job = CreateReadyJobWithRows(
            (2, "3401.BRD.GRV06-K-I", """{"sku":"3401.BRD.GRV06-K-İ","name":"Source","similarSkus":["TARGET-1"]}"""),
            (3, "TARGET-1", """{"sku":"TARGET-1","name":"Target"}"""));
        var repository = new StubRepository(job, []);

        await new ImportPublishingService(repository, TimeProvider.System).PublishAsync(job.Id);

        Assert.Equal(ImportJobStatus.Completed, job.Status);
        Assert.Contains(repository.Relations, relation => relation.Similar.SequenceEqual(["TARGET-1"]));
        Assert.Equal("3401.BRD.GRV06-K-I", repository.Products.Single(product => product.Translations.Single().Name == "Source").SKU);
    }

    private static ImportJob CreateReadyJob(
        params (string SKU, string Name)[] products) =>
        CreateReadyJob(isDryRun: false, products);

    private static ImportJob CreateReadyJob(
        bool isDryRun,
        params (string SKU, string Name)[] products)
    {
        var job = new ImportJob(
            Guid.NewGuid(),
            ImportSourceType.Csv,
            "products.csv",
            new string('A', 64),
            isDryRun);
        job.StartValidation(DateTimeOffset.UtcNow);
        var rowNumber = 2;
        foreach (var product in products)
        {
            var row = job.AddRow(
                Guid.NewGuid(),
                "Products",
                rowNumber++,
                $$"""{"SKU":"{{product.SKU}}","Name":"{{product.Name}}"}""",
                product.SKU);
            row.MarkValid(
                $$"""{"sku":"{{product.SKU}}","name":"{{product.Name}}"}""");
        }

        job.CompleteValidation(DateTimeOffset.UtcNow);
        return job;
    }

    private static ImportJob CreateReadyJobWithRows(
        params (int RowNumber, string SKU, string Payload)[] rows)
    {
        var job = new ImportJob(Guid.NewGuid(), ImportSourceType.Excel, "products.xlsx", new string('E', 64), false);
        job.StartValidation(DateTimeOffset.UtcNow);
        foreach (var row in rows)
        {
            var importRow = job.AddRow(Guid.NewGuid(), "Products", row.RowNumber, "{}", row.SKU);
            importRow.MarkValid(row.Payload);
        }

        job.CompleteValidation(DateTimeOffset.UtcNow);
        return job;
    }

    private sealed class StubRepository(
        ImportJob? job,
        IEnumerable<string> existingSkus)
        : IImportPublishingRepository
    {
        private readonly HashSet<string> _existingSkus = new(existingSkus, StringComparer.OrdinalIgnoreCase);

        public List<Product> Products { get; } = existingSkus.Select(sku =>
        {
            var product = new Product(Guid.NewGuid(), sku);
            product.AddTranslation("tr", "Existing", $"existing-{Guid.NewGuid():N}");
            return product;
        }).ToList();

        public List<ProductAttributeValue> AttributeValues { get; } = [];
        public List<(Guid ProductId, IReadOnlyDictionary<string, string[]> Values)> ImportedAttributes { get; } = [];
        public List<(Guid ProductId, string[] Similar, string[] Complementary)> Relations { get; } = [];
        public List<(Guid ProductId, string VariantKey)> VariantKeys { get; } = [];

        public bool TransactionExecuted { get; private set; }

        public Task<ImportJob?> GetJobAsync(
            Guid jobId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(job);

        public Task<HashSet<string>> GetExistingSkusAsync(
            IReadOnlyCollection<string> skus,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                new HashSet<string>(
                    _existingSkus,
                    StringComparer.OrdinalIgnoreCase));

        public Task<IReadOnlyDictionary<string, Product>> GetProductsBySkusAsync(
            IReadOnlyCollection<string> skus,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<string, Product>>(
                Products.Where(product => skus.Contains(product.SKU, StringComparer.OrdinalIgnoreCase))
                    .ToDictionary(product => product.SKU, StringComparer.OrdinalIgnoreCase));

        public void AddProduct(Product product)
        {
            Products.Add(product);
        }

        public void AddAttributeValue(ProductAttributeValue value)
        {
            AttributeValues.Add(value);
        }

        public void AddIssue(ImportIssue issue)
        {
        }

        public Task PrepareProductDataAsync(IReadOnlyCollection<Guid> productIds,
            IReadOnlyCollection<string> attributeCodes, IReadOnlyCollection<Guid> materialAttributeIds,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ReplaceImportedAttributesAsync(Guid productId, IReadOnlyDictionary<string, string[]> values,
            CancellationToken cancellationToken = default)
        {
            ImportedAttributes.Add((productId, values)); return Task.CompletedTask;
        }
        public Task ReplaceMaterialAsync(Guid productId, Guid? attributeId, Guid? optionId, string? rawValue,
            CancellationToken cancellationToken = default)
        {
            if (attributeId.HasValue && optionId.HasValue)
                AttributeValues.Add(ProductAttributeValue.FromOption(Guid.NewGuid(), productId, attributeId.Value,
                    optionId.Value, rawValue: rawValue));
            return Task.CompletedTask;
        }

        public Task UpsertVariantAsync(Product product, string variantKey, int sortOrder,
            CancellationToken cancellationToken = default)
        {
            VariantKeys.Add((product.Id, variantKey));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<string>> ApplyRelationsAsync(Guid sourceProductId,
            IReadOnlyCollection<string> similarSkus, IReadOnlyCollection<string> complementarySkus,
            CancellationToken cancellationToken = default)
        {
            Relations.Add((sourceProductId, similarSkus.ToArray(), complementarySkus.ToArray()));
            var known = Products.Select(product => product.SKU).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return Task.FromResult<IReadOnlyList<string>>(similarSkus.Concat(complementarySkus)
                .Where(sku => !known.Contains(sku)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
        }

        public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> ApplyRelationsBatchAsync(
            IReadOnlyCollection<ImportRelationRequest> requests,
            CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<Guid, IReadOnlyList<string>>();
            foreach (var request in requests)
                results[request.RowId] = await ApplyRelationsAsync(request.SourceProductId,
                    request.SimilarSkus, request.ComplementarySkus, cancellationToken);
            return results;
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            TransactionExecuted = true;
            await operation(cancellationToken);
        }
    }
}
