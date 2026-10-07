using Ekiphan.Infrastructure.DataImport;
using Ekiphan.Infrastructure.Persistence;
using Ekiphan.Application.DataImport;
using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.DataImport;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.UnitTests.DataImport;

public sealed class ImportPublishingRepositoryTests
{
    [Fact]
    public async Task GetJobForPublishingDoesNotTrackHistoricalValidationIssues()
    {
        await using var db = new EkiphanDbContext(
            new DbContextOptionsBuilder<EkiphanDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options);
        var job = new ImportJob(
            Guid.NewGuid(), ImportSourceType.Excel, "products.xlsx", new string('a', 64), false);
        job.StartValidation(DateTimeOffset.UtcNow);
        var row = job.AddRow(Guid.NewGuid(), "Products", 2, "{}", "SKU-1");
        row.AddIssue(Guid.NewGuid(), ImportIssueSeverity.Warning, "TEST", "Historical warning");
        row.MarkValid("{\"sku\":\"SKU-1\",\"name\":\"Product\"}");
        job.CompleteValidation(DateTimeOffset.UtcNow);
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var loaded = await new ImportPublishingRepository(db).GetJobAsync(job.Id);

        Assert.NotNull(loaded);
        Assert.Empty(db.ChangeTracker.Entries<ImportIssue>());
    }

    [Fact]
    public async Task ReplaceImportedAttributesReusesPendingDefinitionWithinTransaction()
    {
        await using var db = new EkiphanDbContext(
            new DbContextOptionsBuilder<EkiphanDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options);
        var repository = new ImportPublishingRepository(db);

        await repository.ReplaceImportedAttributesAsync(
            Guid.NewGuid(),
            new Dictionary<string, string[]> { ["COLOR"] = ["Red"] });
        await repository.ReplaceImportedAttributesAsync(
            Guid.NewGuid(),
            new Dictionary<string, string[]> { ["COLOR"] = ["Blue"] });
        await repository.SaveChangesAsync();

        Assert.Single(await db.Attributes.Where(item => item.Code == "IMPORT_COLOR").ToListAsync());
    }

    [Fact]
    public async Task ApplyRelationsAddsOneRelationWhenTheSameKeyIsRequestedTwiceBeforeSaving()
    {
        await using var db = CreateDb();
        var (source, target) = await SeedProductsAsync(db, "SOURCE", "TARGET");
        var repository = new ImportPublishingRepository(db);

        await repository.ApplyRelationsAsync(source.Id, [target.SKU, target.SKU], []);
        await repository.ApplyRelationsAsync(source.Id, [target.SKU], []);
        await repository.SaveChangesAsync();

        Assert.Single(await db.ProductRelations.ToListAsync());
    }

    [Fact]
    public async Task ApplyRelationsKeepsDifferentRelationTypesAndTargets()
    {
        await using var db = CreateDb();
        var (source, target) = await SeedProductsAsync(db, "SOURCE", "TARGET");
        var otherTarget = new Product(Guid.NewGuid(), "TARGET-2");
        db.Products.Add(otherTarget);
        await db.SaveChangesAsync();
        var repository = new ImportPublishingRepository(db);

        await repository.ApplyRelationsAsync(source.Id, [target.SKU, otherTarget.SKU], [target.SKU]);
        await repository.SaveChangesAsync();

        var relations = await db.ProductRelations.ToListAsync();
        Assert.Equal(3, relations.Count);
        Assert.Contains(relations, relation => relation.TargetProductId == target.Id && relation.RelationType == ProductRelationType.Similar);
        Assert.Contains(relations, relation => relation.TargetProductId == target.Id && relation.RelationType == ProductRelationType.Complementary);
        Assert.Contains(relations, relation => relation.TargetProductId == otherTarget.Id && relation.RelationType == ProductRelationType.Similar);
    }

    [Fact]
    public async Task ApplyRelationsDoesNotDuplicateAnExistingPersistedRelation()
    {
        await using var db = CreateDb();
        var (source, target) = await SeedProductsAsync(db, "SOURCE", "TARGET");
        db.ProductRelations.Add(new ProductRelation(Guid.NewGuid(), source.Id, target.Id, ProductRelationType.Similar, false));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var repository = new ImportPublishingRepository(db);

        await repository.ApplyRelationsAsync(source.Id, [target.SKU], []);
        await repository.SaveChangesAsync();

        Assert.Single(await db.ProductRelations.ToListAsync());
    }

    [Fact]
    public async Task ApplyRelationsBatchResolvesManyRequestsAndDeduplicatesPendingKeys()
    {
        await using var db = CreateDb();
        var source = new Product(Guid.NewGuid(), "SOURCE");
        var targets = Enumerable.Range(0, 64)
            .Select(index => new Product(Guid.NewGuid(), $"TARGET-{index:D3}"))
            .ToArray();
        db.Products.AddRange([source, .. targets]);
        await db.SaveChangesAsync();
        db.ProductRelations.Add(new ProductRelation(Guid.NewGuid(), source.Id, targets[0].Id,
            ProductRelationType.Similar, false));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var repository = new ImportPublishingRepository(db);
        var requests = Enumerable.Range(0, 256)
            .Select(index => new ImportRelationRequest(Guid.NewGuid(), source.Id,
                [targets[index % targets.Length].SKU], []))
            .ToArray();

        var missing = await repository.ApplyRelationsBatchAsync(requests);
        await repository.SaveChangesAsync();

        Assert.All(missing.Values, value => Assert.Empty(value));
        Assert.Equal(targets.Length, await db.ProductRelations.CountAsync());
    }

    private static EkiphanDbContext CreateDb() => new(
        new DbContextOptionsBuilder<EkiphanDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static async Task<(Product Source, Product Target)> SeedProductsAsync(
        EkiphanDbContext db,
        string sourceSku,
        string targetSku)
    {
        var source = new Product(Guid.NewGuid(), sourceSku);
        var target = new Product(Guid.NewGuid(), targetSku);
        db.Products.AddRange(source, target);
        await db.SaveChangesAsync();
        return (source, target);
    }
}
