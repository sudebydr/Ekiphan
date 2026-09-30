using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Ekiphan.Application.Catalog;
using Ekiphan.Application.Identity;
using Ekiphan.Domain.Catalog;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ekiphan.UnitTests.Api;

public sealed class AdminProductRelationEndpointTests
{
    private const string SigningKey =
        "test-only-signing-key-at-least-32-bytes";

    [Fact]
    public async Task EndpointsFailClosedWithoutJwtConfiguration()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/admin/catalog/products");

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
    }

    [Fact]
    public async Task EndpointsRequireAuthenticationAndCatalogPermission()
    {
        await using var factory = CreateFactory(JwtSettings());
        using var client = factory.CreateClient();

        using var anonymousResponse = await client.GetAsync(
            "/api/admin/catalog/products");
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            anonymousResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateAccessToken("quotes.manage"));
        using var forbiddenResponse = await client.GetAsync(
            "/api/admin/catalog/products");
        Assert.Equal(
            HttpStatusCode.Forbidden,
            forbiddenResponse.StatusCode);
    }

    [Fact]
    public async Task AuthorizedProductLookupBindsSafeFilters()
    {
        var service = new StubService();
        await using var factory = CreateFactory(
            JwtSettings(),
            services =>
            {
                services.RemoveAll<IAdminProductRelationService>();
                services.AddSingleton<IAdminProductRelationService>(service);
            });
        using var client = AuthorizedClient(factory);

        using var response = await client.GetAsync(
            "/api/admin/catalog/products?page=2&pageSize=10" +
            "&language=en&search=plate&isPublished=true" +
            "&missingImage=true&sortBy=sku&sortDirection=asc");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(service.Query);
        Assert.Equal(2, service.Query.Page);
        Assert.Equal(10, service.Query.PageSize);
        Assert.Equal("en", service.Query.LanguageCode);
        Assert.Equal("plate", service.Query.Search);
        Assert.True(service.Query.IsPublished);
        Assert.True(service.Query.MissingGalleryImage);
        Assert.Equal(AdminProductSortBy.SKU, service.Query.SortBy);
        Assert.Equal(
            AdminSortDirection.Ascending,
            service.Query.SortDirection);
        Assert.Contains(
            "no-store",
            response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task CreateBindsNamedRelationTypeAndReturnsCreated()
    {
        var service = new StubService();
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        await using var factory = CreateFactory(
            JwtSettings(),
            services =>
            {
                services.RemoveAll<IAdminProductRelationService>();
                services.AddSingleton<IAdminProductRelationService>(service);
            });
        using var client = AuthorizedClient(factory);

        using var response = await client.PostAsJsonAsync(
            $"/api/admin/catalog/products/{sourceId}/relations?language=tr",
            new
            {
                targetProductId = targetId,
                relationType = "Complementary",
                isBidirectional = true,
                sortOrder = 7,
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(service.CreateCommand);
        Assert.Equal(sourceId, service.CreateCommand.SourceProductId);
        Assert.Equal(targetId, service.CreateCommand.TargetProductId);
        Assert.Equal(
            ProductRelationType.Complementary,
            service.CreateCommand.RelationType);
        Assert.True(service.CreateCommand.IsBidirectional);
        Assert.Equal(7, service.CreateCommand.SortOrder);
    }

    [Theory]
    [InlineData("2")]
    [InlineData("Unknown")]
    public async Task CreateRejectsNumericAndUnknownRelationTypes(
        string relationType)
    {
        var service = new StubService();
        await using var factory = CreateFactory(
            JwtSettings(),
            services =>
            {
                services.RemoveAll<IAdminProductRelationService>();
                services.AddSingleton<IAdminProductRelationService>(service);
            });
        using var client = AuthorizedClient(factory);

        using var response = await client.PostAsJsonAsync(
            $"/api/admin/catalog/products/{Guid.NewGuid()}/relations",
            new
            {
                targetProductId = Guid.NewGuid(),
                relationType,
                isBidirectional = false,
                sortOrder = 0,
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(service.CreateCommand);
    }

    [Fact]
    public async Task DeleteDeactivatesInsteadOfDeleting()
    {
        var service = new StubService();
        var relationId = Guid.NewGuid();
        await using var factory = CreateFactory(
            JwtSettings(),
            services =>
            {
                services.RemoveAll<IAdminProductRelationService>();
                services.AddSingleton<IAdminProductRelationService>(service);
            });
        using var client = AuthorizedClient(factory);

        using var response = await client.DeleteAsync(
            $"/api/admin/catalog/relations/{relationId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(relationId, service.DeactivatedRelationId);
    }

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
                        "Database=AdminProductRelationEndpointTests;" +
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

    private sealed class StubService : IAdminProductRelationService
    {
        public AdminProductListQuery? Query { get; private set; }

        public CreateAdminProductRelationCommand? CreateCommand
        {
            get;
            private set;
        }

        public Guid? DeactivatedRelationId { get; private set; }

        public Task<AdminCatalogProductPage> GetProductsAsync(
            AdminProductListQuery query,
            CancellationToken cancellationToken = default)
        {
            Query = query;
            return Task.FromResult(
                new AdminCatalogProductPage(
                    [],
                    query.Page,
                    query.PageSize,
                    0));
        }

        public Task<IReadOnlyList<AdminProductRelation>> GetRelationsAsync(
            Guid productId,
            string languageCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AdminProductRelation>>([]);

        public Task<IReadOnlyList<Guid>> SelectUnpublishedProductIdsAsync(
            AdminProductBulkSelectionQuery query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<AdminProductRelation> CreateAsync(
            CreateAdminProductRelationCommand command,
            string languageCode,
            CancellationToken cancellationToken = default)
        {
            CreateCommand = command;
            return Task.FromResult(
                new AdminProductRelation(
                    Guid.NewGuid(),
                    command.SourceProductId,
                    command.TargetProductId,
                    command.TargetProductId,
                    "SKU",
                    "Related product",
                    command.RelationType,
                    command.IsBidirectional,
                    false,
                    command.SortOrder));
        }

        public Task DeactivateAsync(
            Guid relationId,
            CancellationToken cancellationToken = default)
        {
            DeactivatedRelationId = relationId;
            return Task.CompletedTask;
        }
    }
}
