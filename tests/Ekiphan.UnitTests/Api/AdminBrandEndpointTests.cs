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

public sealed class AdminBrandEndpointTests
{
    private const string SigningKey =
        "test-only-signing-key-at-least-32-bytes";

    [Fact]
    public async Task EndpointsFailClosedWithoutJwtConfiguration()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/admin/catalog/brands");

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
    }

    [Fact]
    public async Task AuthorizedListBindsFilters()
    {
        var service = new StubService();
        await using var factory = CreateFactory(
            service,
            JwtSettings());
        using var client = AuthorizedClient(factory);

        using var response = await client.GetAsync(
            "/api/admin/catalog/brands?page=2&pageSize=10" +
            "&language=en&search=steel");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, service.Page);
        Assert.Equal(10, service.PageSize);
        Assert.Equal("en", service.Language);
        Assert.Equal("steel", service.Search);
        Assert.Contains(
            "no-store",
            response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task CreateBindsTranslationsAndReturnsCreated()
    {
        var service = new StubService();
        await using var factory = CreateFactory(
            service,
            JwtSettings());
        using var client = AuthorizedClient(factory);

        using var response = await client.PostAsJsonAsync(
            "/api/admin/catalog/brands",
            new
            {
                name = "Steel Brand",
                websiteUrl = "https://example.com",
                sortOrder = 3,
                isPublished = true,
                translations = new[]
                {
                    new
                    {
                        languageCode = "tr",
                        description = "Marka açıklaması",
                        slug = "steel-brand",
                    },
                },
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(service.Created);
        Assert.Equal("Steel Brand", service.Created.Name);
        Assert.Equal(
            "tr",
            Assert.Single(service.Created.Translations).LanguageCode);
    }

    [Fact]
    public async Task DeleteBrandUsesSafeArchiveOperation()
    {
        var service = new StubService();
        await using var factory = CreateFactory(service, JwtSettings());
        using var client = AuthorizedClient(factory);
        var brandId = Guid.NewGuid();

        using var response = await client.DeleteAsync(
            $"/api/admin/catalog/brands/{brandId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(brandId, service.ArchivedBrandId);
    }

    private static HttpClient AuthorizedClient(
        WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateAccessToken());
        return client;
    }

    private static WebApplicationFactory<Program> CreateFactory(
        StubService? service = null,
        IReadOnlyDictionary<string, string?>? settings = null) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder =>
                {
                    builder.UseSetting(
                        "ConnectionStrings:EkiphanDatabase",
                        "Server=(localdb)\\mssqllocaldb;" +
                        "Database=AdminBrandEndpointTests;" +
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
                            if (service is not null)
                            {
                                services.RemoveAll<IAdminBrandService>();
                                services.AddSingleton<IAdminBrandService>(
                                    service);
                            }
                        });
                });

    private static Dictionary<string, string?> JwtSettings() =>
        new()
        {
            ["Authentication:Jwt:Issuer"] = "https://issuer.example",
            ["Authentication:Jwt:Audience"] = "ekiphan-admin",
            ["Authentication:Jwt:SigningKey"] = SigningKey,
        };

    private static string CreateAccessToken()
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
                ["permission"] = "catalog.manage",
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private sealed class StubService : IAdminBrandService
    {
        public int Page { get; private set; }
        public int PageSize { get; private set; }
        public string? Language { get; private set; }
        public string? Search { get; private set; }
        public SaveAdminBrandCommand? Created { get; private set; }
        public Guid? ArchivedBrandId { get; private set; }

        public Task<AdminBrandPage> GetPageAsync(
            int page,
            int pageSize,
            string languageCode,
            string? search,
            CancellationToken cancellationToken = default)
        {
            Page = page;
            PageSize = pageSize;
            Language = languageCode;
            Search = search;
            return Task.FromResult(
                new AdminBrandPage([], page, pageSize, 0));
        }

        public Task<AdminBrandDetail?> GetAsync(
            Guid brandId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AdminBrandDetail?>(null);

        public Task<AdminBrandDetail> CreateAsync(
            SaveAdminBrandCommand command,
            CancellationToken cancellationToken = default)
        {
            Created = command;
            return Task.FromResult(Detail(Guid.NewGuid()));
        }

        public Task<AdminBrandDetail?> UpdateAsync(
            Guid brandId,
            SaveAdminBrandCommand command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AdminBrandDetail?>(Detail(brandId));

        public Task<bool> ArchiveAsync(
            Guid brandId,
            CancellationToken cancellationToken = default)
        {
            ArchivedBrandId = brandId;
            return Task.FromResult(true);
        }

        private static AdminBrandDetail Detail(Guid id) =>
            new(
                id,
                "Steel Brand",
                "https://example.com",
                true,
                3,
                [
                    new AdminBrandTranslation(
                        "tr",
                        "Marka açıklaması",
                        "steel-brand"),
                ],
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow);
    }
}
