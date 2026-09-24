using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Ekiphan.Application.Catalog;
using Ekiphan.Application.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ekiphan.UnitTests.Api;

public sealed class AdminProductManagementEndpointTests
{
    private const string SigningKey =
        "test-only-signing-key-at-least-32-bytes";
    private static readonly Guid CategoryId =
        Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid AttributeId =
        Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid TagId =
        Guid.Parse("33333333-3333-4333-8333-333333333333");

    [Fact]
    public async Task MutationsFailClosedWithoutJwtConfiguration()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/admin/catalog/products",
            Request());

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
    }

    [Fact]
    public async Task EndpointsRequireCatalogPermission()
    {
        await using var factory = CreateFactory(JwtSettings());
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateAccessToken("quotes.manage"));

        using var response = await client.GetAsync(
            $"/api/admin/catalog/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateBindsTranslationsAndReturnsCreated()
    {
        var service = new StubService();
        await using var factory = CreateFactory(
            JwtSettings(),
            services =>
            {
                services.RemoveAll<IAdminProductManagementService>();
                services.AddSingleton<IAdminProductManagementService>(service);
            });
        using var client = AuthorizedClient(factory);

        using var response = await client.PostAsJsonAsync(
            "/api/admin/catalog/products",
            Request());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(service.Created);
        Assert.Equal("SKU-NEW", service.Created.SKU);
        Assert.True(service.Created.IsPublished);
        Assert.Equal("tr", Assert.Single(service.Created.Translations).LanguageCode);
        Assert.Equal(CategoryId, Assert.Single(service.Created.CategoryIds!));
        var attributeValue = Assert.Single(service.Created.AttributeValues!);
        Assert.Equal(AttributeId, attributeValue.AttributeId);
        Assert.Equal("Paslanmaz", attributeValue.TextValue);
        Assert.Equal(TagId, Assert.Single(service.Created.TagIds!));
        var translation = Assert.Single(service.Created.Translations);
        Assert.Equal("Yeni ürün | Ekiphan", translation.MetaTitle);
        Assert.False(translation.NoIndex);
        Assert.Contains(
            "no-store",
            response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task MissingUpdateReturnsNotFound()
    {
        var service = new StubService { ReturnUpdatedProduct = false };
        await using var factory = CreateFactory(
            JwtSettings(),
            services =>
            {
                services.RemoveAll<IAdminProductManagementService>();
                services.AddSingleton<IAdminProductManagementService>(service);
            });
        using var client = AuthorizedClient(factory);

        using var response = await client.PutAsJsonAsync(
            $"/api/admin/catalog/products/{Guid.NewGuid()}",
            Request());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUsesSoftDeleteServiceOperation()
    {
        var service = new StubService();
        var productId = Guid.NewGuid();
        await using var factory = CreateFactory(
            JwtSettings(),
            services =>
            {
                services.RemoveAll<IAdminProductManagementService>();
                services.AddSingleton<IAdminProductManagementService>(service);
            });
        using var client = AuthorizedClient(factory);

        using var response = await client.DeleteAsync(
            $"/api/admin/catalog/products/{productId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(productId, service.DeletedProductId);
    }

    private static object Request() =>
        new
        {
            sku = "SKU-NEW",
            brandId = (Guid?)null,
            isPublished = true,
            categoryIds = new[] { CategoryId },
            primaryCategoryId = CategoryId,
            tagIds = new[] { TagId },
            attributeValues = new[]
            {
                new
                {
                    attributeId = AttributeId,
                    sequence = 0,
                    textValue = "Paslanmaz",
                    numericValue = (decimal?)null,
                    booleanValue = (bool?)null,
                    attributeOptionId = (Guid?)null,
                    unitId = (Guid?)null,
                },
            },
            translations = new[]
            {
                new
                {
                    languageCode = "tr",
                    name = "Yeni ürün",
                    slug = "yeni-urun",
                    shortDescription = "Kısa açıklama",
                    longDescription = "Uzun açıklama",
                    metaTitle = "Yeni ürün | Ekiphan",
                    metaDescription = "Yeni ürün meta açıklaması",
                    canonicalUrl = "https://www.ekiphan.com/katalog/yeni-urun",
                    noIndex = false,
                },
            },
        };

    private static HttpClient AuthorizedClient(
        WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateAccessToken("catalog.manage"));
        return client;
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IReadOnlyDictionary<string, string?>? settings = null,
        Action<IServiceCollection>? configureServices = null) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder =>
                {
                    builder.UseSetting(
                        "ConnectionStrings:EkiphanDatabase",
                        "Server=(localdb)\\mssqllocaldb;" +
                        "Database=AdminProductManagementEndpointTests;" +
                        "Trusted_Connection=True");
                    if (settings is not null)
                    {
                        foreach (var setting in settings)
                        {
                            builder.UseSetting(setting.Key, setting.Value);
                        }
                    }

                    builder.ConfigureTestServices(
                        services =>
                        {
                            services.RemoveAll<IAdminAuthenticationService>();
                            services.AddSingleton<IAdminAuthenticationService,
                                TestAdminAuthenticationService>();
                            configureServices?.Invoke(services);
                        });
                });

    private static Dictionary<string, string?> JwtSettings() =>
        new()
        {
            ["Authentication:Jwt:Issuer"] = "https://issuer.example",
            ["Authentication:Jwt:Audience"] = "ekiphan-admin",
            ["Authentication:Jwt:SigningKey"] = SigningKey,
        };

    private static string CreateAccessToken(string permission)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = "https://issuer.example",
            Audience = "ekiphan-admin",
            Expires = DateTime.UtcNow.AddMinutes(5),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = Guid.NewGuid().ToString(),
                ["jti"] = Guid.NewGuid().ToString("N"),
                ["security_stamp"] =
                    TestAdminAuthenticationService.SecurityStamp,
                ["permission"] = permission,
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private sealed class StubService : IAdminProductManagementService
    {
        public SaveAdminProductCommand? Created { get; private set; }

        public bool ReturnUpdatedProduct { get; init; } = true;

        public Guid? DeletedProductId { get; private set; }

        public Task<AdminProductDetail?> GetAsync(
            Guid productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AdminProductDetail?>(Product(productId));

        public Task<AdminProductDetail> CreateAsync(
            SaveAdminProductCommand command,
            CancellationToken cancellationToken = default)
        {
            Created = command;
            return Task.FromResult(Product(Guid.NewGuid()));
        }

        public Task<AdminProductDetail?> UpdateAsync(
            Guid productId,
            SaveAdminProductCommand command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                ReturnUpdatedProduct
                    ? Product(productId)
                    : null);

        public Task<bool> SoftDeleteAsync(
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            DeletedProductId = productId;
            return Task.FromResult(true);
        }

        private static AdminProductDetail Product(Guid id) =>
            new(
                id,
                "SKU-NEW",
                null,
                true,
                [
                    new AdminProductTranslation(
                        "tr",
                        "Yeni ürün",
                        "yeni-urun",
                        null,
                        null),
                ],
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow);
    }
}
