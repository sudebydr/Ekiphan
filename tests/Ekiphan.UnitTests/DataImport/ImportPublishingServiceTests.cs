using Ekiphan.Application.DataImport;
using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.DataImport;

namespace Ekiphan.UnitTests.DataImport;

public sealed class ImportPublishingServiceTests
{
    [Fact]
    public async Task PublishAsyncCreatesDraftProductsAndSkipsExistingSkus()
    {
        var job = CreateReadyJob(("NEW-1", "Yeni Ürün"), ("OLD-1", "Eski Ürün"));
        var repository = new StubRepository(job, ["OLD-1"]);
        var service = new ImportPublishingService(
            repository,
            TimeProvider.System);

        var result = await service.PublishAsync(job.Id);

        Assert.Equal(ImportJobStatus.Completed, result.Status);
        Assert.Equal(1, result.PublishedRowCount);
        Assert.Equal(1, result.SkippedRowCount);
        var product = Assert.Single(repository.Products);
        Assert.Equal("NEW-1", product.SKU);
        Assert.False(product.IsPublished);
        var translation = Assert.Single(product.Translations);
        Assert.Equal("tr", translation.LanguageCode);
        Assert.Equal("Yeni Ürün", translation.Name);
        Assert.EndsWith(product.Id.ToString("N"), translation.Slug);
        Assert.True(repository.TransactionExecuted);
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
        Assert.Null(product.PrimaryCategoryId);
        Assert.Equal(tagId, Assert.Single(product.Tags).TagId);
        var materialValue = Assert.Single(repository.AttributeValues);
        Assert.Equal(product.Id, materialValue.ProductId);
        Assert.Equal(materialAttributeId, materialValue.AttributeId);
        Assert.Equal(materialOptionId, materialValue.AttributeOptionId);
        Assert.Equal("Çelik", materialValue.RawValue);
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

    private sealed class StubRepository(
        ImportJob? job,
        IEnumerable<string> existingSkus)
        : IImportPublishingRepository
    {
        private readonly HashSet<string> _existingSkus =
            new(existingSkus, StringComparer.OrdinalIgnoreCase);

        public List<Product> Products { get; } = [];

        public List<ProductAttributeValue> AttributeValues { get; } = [];

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

        public void AddProduct(Product product)
        {
            Products.Add(product);
        }

        public void AddAttributeValue(ProductAttributeValue value)
        {
            AttributeValues.Add(value);
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
