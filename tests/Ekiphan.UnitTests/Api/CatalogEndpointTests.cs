using System.Net;
using System.Text.Json;
using Ekiphan.Application.Catalog;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ekiphan.UnitTests.Api;

public sealed class CatalogEndpointTests
{
    [Fact]
    public async Task NavigationIsPublicAndLocalized()
    {
        var service = new StubCatalogQueryService();
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/catalog/en/navigation");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("en", service.NavigationLanguage);
    }

    [Fact]
    public async Task ProductListIsPublicAndBindsFilters()
    {
        var service = new StubCatalogQueryService();
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/catalog/tr/products?page=2&pageSize=10&q=tabak" +
            "&section=urunlerimiz&category=porselen&brand=ekiphan" +
            "&tag=yeni&sort=Newest" +
            "&attribute=11111111-1111-4111-8111-111111111111:o%3A22222222-2222-4222-8222-222222222222");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(service.Query);
        Assert.Equal("tr", service.Query.LanguageCode);
        Assert.Equal(2, service.Query.Page);
        Assert.Equal(10, service.Query.PageSize);
        Assert.Equal("tabak", service.Query.Search);
        Assert.Equal("urunlerimiz", service.Query.SectionSlug);
        Assert.Equal("porselen", service.Query.CategorySlug);
        Assert.Equal("ekiphan", service.Query.BrandSlug);
        Assert.Equal("yeni", service.Query.TagSlug);
        Assert.Equal(CatalogProductSort.Newest, service.Query.Sort);
        Assert.Single(service.Query.AttributeFilters!);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(2, json.RootElement.GetProperty("page").GetInt32());
    }

    [Fact]
    public async Task FacetsRequireCategoryAndAreLocalized()
    {
        var service = new StubCatalogQueryService();
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/catalog/en/facets?category=porcelain");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("en", service.FacetLanguage);
        Assert.Equal("porcelain", service.FacetCategory);
    }

    [Fact]
    public async Task SitemapProjectionIsPublicAndLocalized()
    {
        var updatedAt = DateTimeOffset.UtcNow;
        var service = new StubCatalogQueryService
        {
            SitemapEntries =
            [
                new CatalogSitemapEntry("servis-tabagi", updatedAt),
            ],
        };
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/catalog/en/sitemap");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("en", service.SitemapLanguage);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(
            "servis-tabagi",
            json.RootElement[0].GetProperty("slug").GetString());
    }

    [Fact]
    public async Task BrandListIsPublicAndLocalized()
    {
        var service = new StubCatalogQueryService
        {
            Brands =
            [
                new CatalogBrandListItem(
                    Guid.NewGuid(),
                    "Sample",
                    "sample",
                    "Açıklama",
                    "https://example.com/",
                    null,
                    DateTimeOffset.UtcNow),
            ],
        };
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/catalog/en/brands");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("en", service.BrandListLanguage);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(
            "sample",
            json.RootElement[0].GetProperty("slug").GetString());
    }

    [Fact]
    public async Task BrandDetailReturnsPublicProjection()
    {
        var brandId = Guid.NewGuid();
        var service = new StubCatalogQueryService
        {
            BrandDetail = new CatalogBrandDetail(
                brandId,
                "Sample",
                "sample",
                "Açıklama",
                "https://example.com/",
                null,
                [],
                [],
                [],
                DateTimeOffset.UtcNow),
        };
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/catalog/tr/brands/sample");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("tr", service.BrandDetailLanguage);
        Assert.Equal("sample", service.BrandDetailSlug);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(
            brandId,
            json.RootElement.GetProperty("id").GetGuid());
    }

    [Theory]
    [InlineData("/api/catalog/de/products")]
    [InlineData("/api/catalog/tr/products?page=0")]
    [InlineData("/api/catalog/tr/products?pageSize=101")]
    [InlineData("/api/catalog/tr/products?sort=1")]
    [InlineData("/api/catalog/tr/products?q=%20")]
    [InlineData("/api/catalog/tr/products?q=a")]
    [InlineData("/api/catalog/tr/products?attribute=invalid")]
    [InlineData("/api/catalog/tr/facets")]
    public async Task ProductListRejectsInvalidInput(string path)
    {
        await using var factory = CreateFactory(new StubCatalogQueryService());
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProductDetailReturnsLocalizedPublishedProjection()
    {
        var productId = Guid.NewGuid();
        var service = new StubCatalogQueryService
        {
            Detail = new CatalogProductDetail(
                productId,
                "SKU-1",
                "Servis Tabağı",
                "servis-tabagi",
                "Kısa açıklama",
                "Uzun açıklama",
                null,
                [],
                [],
                [],
                [
                    new CatalogProductSummary(
                        Guid.NewGuid(),
                        "SKU-2",
                        "Benzer Tabak",
                        "benzer-tabak",
                        null,
                        null,
                        null,
                        DateTimeOffset.UtcNow),
                ],
                [
                    new CatalogProductSummary(
                        Guid.NewGuid(),
                        "SKU-3",
                        "Tamamlayıcı Kase",
                        "tamamlayici-kase",
                        null,
                        null,
                        null,
                        DateTimeOffset.UtcNow),
                ],
                [
                    new CatalogImage(
                        Guid.NewGuid(),
                        "https://cdn.example.com/products/servis-tabagi.webp",
                        "Beyaz servis tabağı"),
                ],
                DateTimeOffset.UtcNow),
        };
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/catalog/tr/products/servis-tabagi");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("tr", service.DetailLanguage);
        Assert.Equal("servis-tabagi", service.DetailSlug);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(
            productId,
            json.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(
            "Benzer Tabak",
            json.RootElement
                .GetProperty("similarProducts")[0]
                .GetProperty("name")
                .GetString());
        Assert.Equal(
            "Tamamlayıcı Kase",
            json.RootElement
                .GetProperty("complementaryProducts")[0]
                .GetProperty("name")
                .GetString());
        Assert.Equal(
            "Beyaz servis tabağı",
            json.RootElement
                .GetProperty("images")[0]
                .GetProperty("altText")
                .GetString());
    }

    [Fact]
    public async Task MissingProductReturnsNotFound()
    {
        await using var factory = CreateFactory(new StubCatalogQueryService());
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/catalog/en/products/missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        ICatalogQueryService service) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder =>
                {
                    builder.UseSetting(
                        "ConnectionStrings:EkiphanDatabase",
                        "Server=(localdb)\\mssqllocaldb;" +
                        "Database=CatalogEndpointTests;" +
                        "Trusted_Connection=True");
                    builder.ConfigureTestServices(
                        services =>
                        {
                            services.RemoveAll<ICatalogQueryService>();
                            services.AddSingleton(service);
                        });
                });

    private sealed class StubCatalogQueryService : ICatalogQueryService
    {
        public string? NavigationLanguage { get; private set; }

        public CatalogProductQuery? Query { get; private set; }

        public CatalogProductDetail? Detail { get; init; }

        public IReadOnlyList<CatalogSitemapEntry> SitemapEntries { get; init; } =
            [];

        public string? SitemapLanguage { get; private set; }

        public string? DetailLanguage { get; private set; }

        public string? DetailSlug { get; private set; }

        public IReadOnlyList<CatalogBrandListItem> Brands { get; init; } = [];

        public CatalogBrandDetail? BrandDetail { get; init; }

        public string? BrandListLanguage { get; private set; }

        public string? BrandDetailLanguage { get; private set; }

        public string? BrandDetailSlug { get; private set; }

        public string? FacetLanguage { get; private set; }

        public string? FacetCategory { get; private set; }

        public Task<CatalogNavigation> GetNavigationAsync(
            string languageCode,
            CancellationToken cancellationToken = default)
        {
            NavigationLanguage = languageCode;
            return Task.FromResult(
                new CatalogNavigation([], [], [], []));
        }

        public Task<CatalogPagedResult<CatalogProductSummary>> GetProductsAsync(
            CatalogProductQuery query,
            CancellationToken cancellationToken = default)
        {
            Query = query;
            return Task.FromResult(
                new CatalogPagedResult<CatalogProductSummary>(
                    [],
                    query.Page,
                    query.PageSize,
                    0));
        }

        public Task<IReadOnlyList<CatalogFacet>> GetFacetsAsync(
            string languageCode,
            string categorySlug,
            CancellationToken cancellationToken = default)
        {
            FacetLanguage = languageCode;
            FacetCategory = categorySlug;
            return Task.FromResult<IReadOnlyList<CatalogFacet>>([]);
        }

        public Task<IReadOnlyList<CatalogSitemapEntry>> GetSitemapEntriesAsync(
            string languageCode,
            CancellationToken cancellationToken = default)
        {
            SitemapLanguage = languageCode;
            return Task.FromResult(SitemapEntries);
        }

        public Task<IReadOnlyList<CatalogBrandListItem>> GetBrandsAsync(
            string languageCode,
            CancellationToken cancellationToken = default)
        {
            BrandListLanguage = languageCode;
            return Task.FromResult(Brands);
        }

        public Task<CatalogBrandDetail?> GetBrandAsync(
            string languageCode,
            string slug,
            CancellationToken cancellationToken = default)
        {
            BrandDetailLanguage = languageCode;
            BrandDetailSlug = slug;
            return Task.FromResult(BrandDetail);
        }

        public Task<CatalogProductDetail?> GetProductAsync(
            string languageCode,
            string slug,
            CancellationToken cancellationToken = default)
        {
            DetailLanguage = languageCode;
            DetailSlug = slug;
            return Task.FromResult(Detail);
        }
    }
}
