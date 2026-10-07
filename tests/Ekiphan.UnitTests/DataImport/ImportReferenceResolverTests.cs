using Ekiphan.Application.DataImport;
using Ekiphan.Domain.Catalog;
using Ekiphan.Infrastructure.DataImport;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.UnitTests.DataImport;

public sealed class ImportReferenceResolverTests
{
    private static readonly string[] FirstPath = ["MUTFAK", "TABAK"];
    private static readonly string[] SecondPath = ["SUNUM", "TABAK"];

    [Fact]
    public async Task ResolveNormalizesAndUpsertsBrandWithoutDuplicates()
    {
        await using var db = Context();
        db.Brands.Add(new Brand(Guid.NewGuid(), "Ornek Marka"));
        await db.SaveChangesAsync();
        var resolver = new ImportReferenceResolver(db);

        var first = await resolver.ResolveAsync(["  ORNEK MARKA  ", "Yeni Marka"], [], [], []);
        await db.SaveChangesAsync();
        var second = await resolver.ResolveAsync(["ornek marka", " yeni marka "], [], [], []);
        await db.SaveChangesAsync();

        Assert.Equal(first.BrandIds["  ORNEK MARKA  "], second.BrandIds["ornek marka"]);
        Assert.Equal(2, await db.Brands.CountAsync());
    }

    [Fact]
    public async Task ResolveUpsertsMaterialAttributeAndOptionWithoutDuplicates()
    {
        await using var db = Context();
        var resolver = new ImportReferenceResolver(db);
        var first = await resolver.ResolveAsync([], [], ["Seramik"], []);
        await db.SaveChangesAsync();
        var second = await resolver.ResolveAsync([], [], [" seramik "], []);
        await db.SaveChangesAsync();

        Assert.Equal(first.MaterialAttributeId, second.MaterialAttributeId);
        Assert.Equal(first.MaterialOptionIds["Seramik"], second.MaterialOptionIds[" seramik "]);
        Assert.Single(await db.Set<AttributeOption>().ToListAsync());
    }

    [Fact]
    public async Task ResolveUsesFullHierarchyForSameChildUnderDifferentParents()
    {
        await using var db = Context();
        var section = Guid.NewGuid();
        var firstRoot = Category(section, null, "Mutfak");
        var secondRoot = Category(section, null, "Sunum");
        var firstLeaf = Category(section, firstRoot.Id, "Tabak");
        var secondLeaf = Category(section, secondRoot.Id, "Tabak");
        db.Categories.AddRange(firstRoot, secondRoot, firstLeaf, secondLeaf);
        await db.SaveChangesAsync();

        var result = await new ImportReferenceResolver(db).ResolveAsync([], [FirstPath, SecondPath], [], []);

        Assert.Equal([firstRoot.Id, firstLeaf.Id], result.CategoryPathIds[ImportCategoryPath.Key(FirstPath)]);
        Assert.Equal([secondRoot.Id, secondLeaf.Id], result.CategoryPathIds[ImportCategoryPath.Key(SecondPath)]);
    }

    [Fact]
    public async Task ResolveSelectsPublishedCanonicalPathWhenExactPathIsDuplicated()
    {
        await using var db = Context();
        var section = Guid.NewGuid();
        var inactiveRoot = Category(section, null, "Urunler", false);
        var activeRoot = Category(section, null, " URUNLER ", true);
        var inactiveLeaf = Category(section, inactiveRoot.Id, "Alt Grup", false);
        var canonicalLeaf = Category(section, activeRoot.Id, "ALT GRUP", false);
        db.Categories.AddRange(inactiveRoot, activeRoot, inactiveLeaf, canonicalLeaf);
        await db.SaveChangesAsync();
        string[] path = ["urunler", "alt grup"];

        var result = await new ImportReferenceResolver(db).ResolveAsync([], [path], [], []);

        Assert.Equal([activeRoot.Id, canonicalLeaf.Id], result.CategoryPathIds[ImportCategoryPath.Key(path)]);
        Assert.Equal(4, await db.Categories.CountAsync());
    }

    [Fact]
    public async Task ResolveCreatesUnknownHierarchyOnceUnderExistingSection()
    {
        await using var db = Context();
        var section = new ProductSection(Guid.NewGuid(), "PRODUCTS");
        db.ProductSections.Add(section);
        await db.SaveChangesAsync();
        string[] path = ["Yeni Ana", "Yeni Alt"];
        var resolver = new ImportReferenceResolver(db);

        var first = await resolver.ResolveAsync([], [path], [], []);
        await db.SaveChangesAsync();
        var second = await resolver.ResolveAsync([], [path], [], []);
        await db.SaveChangesAsync();

        Assert.Equal(first.CategoryPathIds[ImportCategoryPath.Key(path)], second.CategoryPathIds[ImportCategoryPath.Key(path)]);
        Assert.Equal(2, await db.Categories.CountAsync());
    }

    private static Category Category(Guid section, Guid? parent, string name, bool published = true)
    {
        var category = new Category(Guid.NewGuid(), section, parent);
        category.AddTranslation("tr", name, $"category-{Guid.NewGuid():N}");
        category.SetPublished(published);
        return category;
    }

    private static EkiphanDbContext Context() => new(new DbContextOptionsBuilder<EkiphanDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
}
