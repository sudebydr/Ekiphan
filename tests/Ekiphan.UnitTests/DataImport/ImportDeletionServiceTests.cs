using System.Data.Common;
using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.DataImport;
using Ekiphan.Infrastructure.DataImport;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Ekiphan.UnitTests.DataImport;

// Opt-in integration tests: never use application/RDS/production configuration.
public sealed class LocalImportDeletionFactAttribute : FactAttribute
{
    public LocalImportDeletionFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EKIPHAN_LOCAL_IMPORT_DELETE_TEST_CONNECTION")))
            Skip = "Requires explicitly verified LOCAL SQL Server test connection.";
    }
}

public sealed class ImportDeletionServiceTests
{
    [LocalImportDeletionFact]
    public async Task DeletesOnlyOwnedProductAndHistoryRestoringIncomingTypesAndSortOrders()
    {
        await using var fixture = new LocalFixture();
        var job = fixture.AddCompletedJob("target.xlsx", "TARGET");
        var otherJob = fixture.AddCompletedJob("source.xlsx", "SOURCE");
        var target = fixture.AddProduct(job, "TARGET");
        var source = fixture.AddProduct(otherJob, "SOURCE");
        target.AddTranslation("tr", "Target", fixture.Prefix + "target");
        fixture.Db.ProductRelations.AddRange(
            new ProductRelation(Guid.NewGuid(), source.Id, target.Id, ProductRelationType.Similar, false, 7),
            new ProductRelation(Guid.NewGuid(), source.Id, target.Id, ProductRelationType.Complementary, false, 12),
            new ProductRelation(Guid.NewGuid(), target.Id, source.Id, ProductRelationType.Similar, false));
        fixture.Db.PendingProductRelations.Add(new PendingProductRelation(target.Id, fixture.Prefix + "MISSING", ProductRelationType.Similar, 3));
        await fixture.Db.SaveChangesAsync();

        var result = await new ImportDeletionService(fixture.Db).DeleteAsync(job.Id);
        fixture.Db.ChangeTracker.Clear();

        Assert.Equal(1, result.DeletedProducts);
        Assert.Equal(2, result.RestoredPendingRelations);
        Assert.False(await fixture.Db.Products.IgnoreQueryFilters().AnyAsync(item => item.Id == target.Id));
        Assert.False(await fixture.Db.ImportJobs.AnyAsync(item => item.Id == job.Id));
        Assert.False(await fixture.Db.Set<ImportRow>().AnyAsync(item => item.ImportJobId == job.Id));
        Assert.False(await fixture.Db.Set<ImportIssue>().AnyAsync(item => item.Id == fixture.IssueIds[job.Id]));
        Assert.True(await fixture.Db.ImportJobs.AnyAsync(item => item.Id == otherJob.Id));
        Assert.True(await fixture.Db.Products.AnyAsync(item => item.Id == source.Id));
        Assert.False(await fixture.Db.Set<ProductTranslation>().AnyAsync(item => item.ProductId == target.Id));
        var pending = await fixture.Db.PendingProductRelations.Where(item => item.SourceProductId == source.Id).ToListAsync();
        Assert.Equal(2, pending.Count);
        Assert.Contains(pending, item => item.TargetNormalizedSku == target.NormalizedSku && item.RelationType == ProductRelationType.Similar && item.SortOrder == 7);
        Assert.Contains(pending, item => item.RelationType == ProductRelationType.Complementary && item.SortOrder == 12);
    }

    [LocalImportDeletionFact]
    public async Task UnprovenLegacyOwnershipBlocksDeletionWithoutChangingData()
    {
        await using var fixture = new LocalFixture();
        var job = fixture.AddCompletedJob("legacy.xlsx", "LEGACY");
        var product = fixture.AddProduct(null, "LEGACY");
        await fixture.Db.SaveChangesAsync();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => new ImportDeletionService(fixture.Db).DeleteAsync(job.Id));
        Assert.Contains("Sahiplik", exception.Message);
        Assert.True(await fixture.Db.Products.AnyAsync(item => item.Id == product.Id));
        Assert.True(await fixture.Db.ImportJobs.AnyAsync(item => item.Id == job.Id));
    }

    [LocalImportDeletionFact]
    public async Task SharedSkuBlocksOwnerDeletionAndPreservesBothImports()
    {
        await using var fixture = new LocalFixture();
        var owner = fixture.AddCompletedJob("owner.xlsx", "SHARED");
        var updater = fixture.AddCompletedJob("updater.xlsx", "SHARED");
        var product = fixture.AddProduct(owner, "SHARED");
        await fixture.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ImportDeletionService(fixture.Db).DeleteAsync(owner.Id));
        Assert.True(await fixture.Db.Products.AnyAsync(item => item.Id == product.Id));
        Assert.True(await fixture.Db.ImportJobs.AnyAsync(item => item.Id == updater.Id));

        // The updater has proven non-ownership: removing its history must not remove the product.
        var result = await new ImportDeletionService(fixture.Db).DeleteAsync(updater.Id);
        Assert.Equal(0, result.DeletedProducts);
        Assert.True(await fixture.Db.Products.AnyAsync(item => item.Id == product.Id));
    }

    [LocalImportDeletionFact]
    public async Task PendingSortConflictRollsBackAndKeepsOriginalRelation()
    {
        await using var fixture = new LocalFixture();
        var job = fixture.AddCompletedJob("conflict.xlsx", "TARGET");
        var target = fixture.AddProduct(job, "TARGET");
        var source = fixture.AddProduct(null, "SOURCE");
        fixture.Db.ProductRelations.Add(new ProductRelation(Guid.NewGuid(), source.Id, target.Id, ProductRelationType.Similar, false, 7));
        fixture.Db.PendingProductRelations.Add(new PendingProductRelation(source.Id, target.SKU, ProductRelationType.Similar, 8));
        await fixture.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ImportDeletionService(fixture.Db).DeleteAsync(job.Id));
        Assert.True(await fixture.Db.Products.AnyAsync(item => item.Id == target.Id));
        Assert.Equal(1, await fixture.Db.ProductRelations.CountAsync(item => item.TargetProductId == target.Id));
    }

    [LocalImportDeletionFact]
    public async Task ExistingIdenticalPendingIsNotDuplicated()
    {
        await using var fixture = new LocalFixture();
        var job = fixture.AddCompletedJob("dedup.xlsx", "TARGET");
        var target = fixture.AddProduct(job, "TARGET");
        var source = fixture.AddProduct(null, "SOURCE");
        fixture.Db.ProductRelations.Add(new ProductRelation(Guid.NewGuid(), source.Id, target.Id, ProductRelationType.Similar, false, 7));
        fixture.Db.PendingProductRelations.Add(new PendingProductRelation(source.Id, target.SKU, ProductRelationType.Similar, 7));
        await fixture.Db.SaveChangesAsync();
        var result = await new ImportDeletionService(fixture.Db).DeleteAsync(job.Id);
        Assert.Equal(0, result.RestoredPendingRelations);
        Assert.Equal(1, await fixture.Db.PendingProductRelations.CountAsync(item => item.SourceProductId == source.Id));
    }

    [LocalImportDeletionFact]
    public async Task DatabaseFailureAfterPendingSaveRollsBackAllChanges()
    {
        await using var fixture = new LocalFixture();
        var job = fixture.AddCompletedJob("rollback.xlsx", "TARGET");
        var target = fixture.AddProduct(job, "TARGET");
        var source = fixture.AddProduct(null, "SOURCE");
        fixture.Db.ProductRelations.Add(new ProductRelation(Guid.NewGuid(), source.Id, target.Id, ProductRelationType.Similar, false, 7));
        await fixture.Db.SaveChangesAsync();
        await using var failingDb = fixture.NewContext(new FailProductDeleteInterceptor());
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ImportDeletionService(failingDb).DeleteAsync(job.Id));
        fixture.Db.ChangeTracker.Clear();
        Assert.True(await fixture.Db.Products.AnyAsync(item => item.Id == target.Id));
        Assert.True(await fixture.Db.ImportJobs.AnyAsync(item => item.Id == job.Id));
        Assert.Equal(1, await fixture.Db.ProductRelations.CountAsync(item => item.TargetProductId == target.Id));
        Assert.False(await fixture.Db.PendingProductRelations.AnyAsync(item => item.SourceProductId == source.Id));
    }

    private sealed class FailProductDeleteInterceptor : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("DELETE", StringComparison.Ordinal) && command.CommandText.Contains("[Products]", StringComparison.Ordinal))
                throw new InvalidOperationException("Synthetic delete failure after pending save");
            return new ValueTask<InterceptionResult<int>>(result);
        }
    }

    private sealed class LocalFixture : IAsyncDisposable
    {
        private readonly string connection;
        private readonly List<Guid> jobs = [];
        private readonly List<Guid> products = [];
        public string Prefix { get; } = "DELETE-TEST-" + Guid.NewGuid().ToString("N") + "-";
        public Dictionary<Guid, Guid> IssueIds { get; } = [];
        public EkiphanDbContext Db { get; }

        public LocalFixture()
        {
            connection = Environment.GetEnvironmentVariable("EKIPHAN_LOCAL_IMPORT_DELETE_TEST_CONNECTION")!;
            var builder = new SqlConnectionStringBuilder(connection);
            if (builder.DataSource != @".\SQLEXPRESS" || builder.InitialCatalog != "EkiphanDevelopmentLocal")
                throw new InvalidOperationException("Tests only allow LOCAL EkiphanDevelopmentLocal.");
            Db = NewContext();
        }

        public EkiphanDbContext NewContext(DbCommandInterceptor? interceptor = null)
        {
            var options = new DbContextOptionsBuilder<EkiphanDbContext>().UseSqlServer(connection);
            if (interceptor != null) options.AddInterceptors(interceptor);
            return new EkiphanDbContext(options.Options);
        }

        public ImportJob AddCompletedJob(string filename, string sku)
        {
            var job = new ImportJob(Guid.NewGuid(), ImportSourceType.Excel, filename,
                Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Guid.NewGuid().ToByteArray())), false);
            job.StartValidation(DateTimeOffset.UtcNow);
            var row = job.AddRow(Guid.NewGuid(), "Test", 2, "{}", Prefix + sku);
            row.MarkValid("{}");
            var issueId = Guid.NewGuid();
            row.AddIssue(issueId, ImportIssueSeverity.Warning, "TEST", "Synthetic test issue");
            IssueIds[job.Id] = issueId;
            job.CompleteValidation(DateTimeOffset.UtcNow);
            job.StartPublishing(DateTimeOffset.UtcNow);
            row.MarkPublished();
            job.CompletePublishing(DateTimeOffset.UtcNow);
            Db.ImportJobs.Add(job);
            jobs.Add(job.Id);
            return job;
        }

        public Product AddProduct(ImportJob? owner, string sku)
        {
            var product = new Product(Guid.NewGuid(), Prefix + sku);
            if (owner != null) product.SetImportCreationOwner(owner.Id);
            Db.Products.Add(product);
            products.Add(product.Id);
            return product;
        }

        public async ValueTask DisposeAsync()
        {
            Db.ChangeTracker.Clear();
            await Db.ProductRelations.Where(item => products.Contains(item.SourceProductId) || products.Contains(item.TargetProductId)).ExecuteDeleteAsync();
            await Db.PendingProductRelations.Where(item => products.Contains(item.SourceProductId)).ExecuteDeleteAsync();
            await Db.Products.IgnoreQueryFilters().Where(item => products.Contains(item.Id)).ExecuteDeleteAsync();
            await Db.ImportJobs.Where(item => jobs.Contains(item.Id)).ExecuteDeleteAsync();
            await Db.DisposeAsync();
        }
    }
}
