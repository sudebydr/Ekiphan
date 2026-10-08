using Ekiphan.Application.Catalog;
using Ekiphan.Domain.Catalog;
using Ekiphan.Infrastructure.Catalog;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.UnitTests.Catalog;

public sealed class PublicCatalogQueryTests
{
    private static EkiphanDbContext CreateDb() => new(new DbContextOptionsBuilder<EkiphanDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private sealed class Urls : IPublicMediaUrlResolver
    {
        public string? Resolve(string? key) => key is null ? null : $"/media/{key}";
    }
    private static Product Product(string sku, Guid? brand = null)
    {
        var product = new Product(Guid.NewGuid(), sku, brand);
        product.AddTranslation("tr", "Türkçe ürün", sku.ToLowerInvariant());
        product.SetPublished(true);
        return product;
    }

    [Fact]
    public async Task EnglishUsesTranslationOrTurkishFallbackWithoutDuplicatingProducts()
    {
        await using var db = CreateDb();
        var translated = Product("TRANSLATED");
        translated.AddTranslation("en", "English product", "english-product");
        var fallback = Product("FALLBACK");
        db.Products.AddRange(translated, fallback);
        await db.SaveChangesAsync();
        var service = new CatalogQueryService(db, new Urls());
        var result = await service.GetProductsAsync(new("en"));
        Assert.Equal(2, result.TotalCount);
        Assert.Equal("English product", result.Items.Single(item => item.Id == translated.Id).Name);
        Assert.Equal("Türkçe ürün", result.Items.Single(item => item.Id == fallback.Id).Name);
        Assert.Equal("Türkçe ürün", (await service.GetProductAsync("en", "fallback"))!.Name);
    }

    [Fact]
    public async Task FourLevelMembershipIncludesDescendantsAfterLeafSibling()
    {
        await using var db = CreateDb();
        var section = new ProductSection(Guid.NewGuid(), "PRODUCTS");
        section.SetPublished(true);
        section.AddTranslation("tr", "Ürünler", "urunler");
        var root = new Category(Guid.NewGuid(), section.Id);
        root.AddTranslation("tr", "Ana", "ana");
        var leafSibling = new Category(Guid.NewGuid(), section.Id, root.Id);
        leafSibling.AddTranslation("tr", "Yaprak", "yaprak");
        var branch = new Category(Guid.NewGuid(), section.Id, root.Id);
        branch.AddTranslation("tr", "Alt", "alt");
        var third = new Category(Guid.NewGuid(), section.Id, branch.Id);
        third.AddTranslation("tr", "Üç", "uc");
        var fourth = new Category(Guid.NewGuid(), section.Id, third.Id);
        fourth.AddTranslation("tr", "Dört", "dort");
        var product = Product("DEEP");
        product.AddCategory(root.Id, true);
        product.AddCategory(fourth.Id);
        db.ProductSections.Add(section);
        db.Categories.AddRange(root, leafSibling, branch, third, fourth);
        db.Products.Add(product);
        await db.SaveChangesAsync();
        var service = new CatalogQueryService(db, new Urls());
        foreach (var slug in new[] { "ana", "alt", "uc", "dort" })
            Assert.Equal(1, (await service.GetProductsAsync(new("en", CategorySlug: slug))).TotalCount);
        Assert.Equal(4, (await service.GetNavigationAsync("en")).Categories.Count);
    }

    [Fact]
    public async Task NavigationIncludesProductBearingUnpublishedBrandWithoutTranslationAndHidesEmptyBrand()
    {
        await using var db = CreateDb();
        var used = new Brand(Guid.NewGuid(), "Used brand");
        var empty = new Brand(Guid.NewGuid(), "Empty brand");
        empty.SetPublished(true);
        empty.AddTranslation("tr", "Description", "empty");
        db.Brands.AddRange(used, empty);
        db.Products.Add(Product("BRAND", used.Id));
        await db.SaveChangesAsync();
        var service = new CatalogQueryService(db, new Urls());
        var brand = Assert.Single((await service.GetNavigationAsync("en")).Brands);
        Assert.Equal(used.Id.ToString(), brand.Slug);
        Assert.Equal(1, (await service.GetProductsAsync(new("en", BrandSlug: brand.Slug))).TotalCount);
        Assert.False(used.IsPublished);
    }

    [Theory]
    [InlineData("tr")]
    [InlineData("en")]
    public async Task UsageAreaUsesImportedAttributeAndCombinesWithBrand(string language)
    {
        await using var db = CreateDb();
        var brand = new Brand(Guid.NewGuid(), "Used");
        var product = Product("USAGE", brand.Id);
        var other = Product("OTHER");
        var usage = new AttributeDefinition(Guid.NewGuid(), "IMPORT_USAGE_AREA", AttributeDataType.Text);
        var seo = new AttributeDefinition(Guid.NewGuid(), "IMPORT_SEARCH", AttributeDataType.Text);
        db.Attributes.AddRange(usage, seo);
        db.Brands.Add(brand);
        db.Products.AddRange(product, other);
        db.ProductAttributeValues.AddRange(
            ProductAttributeValue.FromText(Guid.NewGuid(), product.Id, usage.Id, "OTEL"),
            ProductAttributeValue.FromText(Guid.NewGuid(), other.Id, seo.Id, "SEO TITLE"));
        await db.SaveChangesAsync();
        var service = new CatalogQueryService(db, new Urls());
        Assert.Equal("OTEL", Assert.Single((await service.GetNavigationAsync(language)).UsageAreas!).Name);
        var result = await service.GetProductsAsync(new(language, BrandSlug: brand.Id.ToString(), UsageArea: "OTEL"));
        Assert.Equal(product.Id, Assert.Single(result.Items).Id);
        Assert.Equal(0, (await service.GetProductsAsync(new(language, UsageArea: "SEO TITLE"))).TotalCount);
    }

    [Fact]
    public async Task RelatedProductsAreDistinctAndPendingIsNotReturned()
    {
        await using var db = CreateDb();
        var source = Product("SOURCE");
        var target = Product("TARGET");
        db.Products.AddRange(source, target);
        db.ProductRelations.AddRange(
            new ProductRelation(Guid.NewGuid(), source.Id, target.Id, ProductRelationType.Similar, true),
            new ProductRelation(Guid.NewGuid(), target.Id, source.Id, ProductRelationType.Similar, true));
        db.PendingProductRelations.Add(new PendingProductRelation(source.Id, "MISSING", ProductRelationType.Similar, 1));
        await db.SaveChangesAsync();
        var detail = await new CatalogQueryService(db, new Urls()).GetProductAsync("en", "source");
        Assert.Equal(target.Id, Assert.Single(detail!.SimilarProducts).Id);
        Assert.DoesNotContain(detail.SimilarProducts, item => item.Id == source.Id);
    }

    [Theory]
    [InlineData(CatalogProductSort.Name)]
    [InlineData(CatalogProductSort.NameDescending)]
    [InlineData(CatalogProductSort.Newest)]
    public async Task PaginationAndSortKeepFallbackProducts(CatalogProductSort sort)
    {
        await using var db = CreateDb();
        db.Products.AddRange(Product("ONE"), Product("TWO"));
        await db.SaveChangesAsync();
        var service = new CatalogQueryService(db, new Urls());
        var first = await service.GetProductsAsync(new("en", PageSize: 1, Sort: sort));
        var second = await service.GetProductsAsync(new("en", Page: 2, PageSize: 1, Sort: sort));
        Assert.Equal(2, first.TotalCount);
        Assert.NotEqual(Assert.Single(first.Items).Id, Assert.Single(second.Items).Id);
    }
}
