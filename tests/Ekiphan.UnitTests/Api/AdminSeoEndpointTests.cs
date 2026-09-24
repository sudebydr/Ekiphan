using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Ekiphan.Application.Identity;
using Ekiphan.Application.Seo;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ekiphan.UnitTests.Api;

public sealed class AdminSeoEndpointTests
{
    private const string SigningKey = "test-only-signing-key-at-least-32-bytes";

    [Fact]
    public async Task EndpointsFailClosedWithoutJwtConfiguration()
    {
        await using var factory = CreateFactory();
        using var response = await factory.CreateClient().GetAsync("/api/admin/seo");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task CatalogManagerCanReadCatalogSeo()
    {
        var service = new StubService();
        await using var factory = CreateFactory(service, JwtSettings());
        using var client = AuthorizedClient(factory, "catalog.manage");
        using var response = await client.GetAsync("/api/admin/seo?contentType=Product&language=tr&page=2&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(service.Query);
        Assert.Equal(SeoContentType.Product, service.Query.ContentType);
        Assert.Equal(2, service.Query.Page);
        Assert.True(service.Scope?.Catalog);
        Assert.False(service.Scope?.Content);
    }

    [Fact]
    public async Task QuoteViewerCannotAccessSeo()
    {
        await using var factory = CreateFactory(new StubService(), JwtSettings());
        using var client = AuthorizedClient(factory, "quotes.read");
        using var response = await client.GetAsync("/api/admin/seo");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static HttpClient AuthorizedClient(WebApplicationFactory<Program> factory, string permission)
    {
        var client = factory.CreateClient();
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = "https://issuer.example", Audience = "ekiphan-admin",
            Expires = DateTime.UtcNow.AddMinutes(5),
            Claims = new Dictionary<string, object> {
                ["sub"] = Guid.NewGuid().ToString(), ["jti"] = Guid.NewGuid().ToString("N"),
                ["security_stamp"] = TestAdminAuthenticationService.SecurityStamp,
                ["permission"] = permission },
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)), SecurityAlgorithms.HmacSha256)
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JsonWebTokenHandler().CreateToken(descriptor));
        return client;
    }

    private static WebApplicationFactory<Program> CreateFactory(StubService? service = null, IReadOnlyDictionary<string, string?>? settings = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:EkiphanDatabase", "Server=(localdb)\\mssqllocaldb;Database=AdminSeoEndpointTests;Trusted_Connection=True");
            if (settings is not null) foreach (var setting in settings) builder.UseSetting(setting.Key, setting.Value);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAdminAuthenticationService>();
                services.AddSingleton<IAdminAuthenticationService, TestAdminAuthenticationService>();
                if (service is not null) { services.RemoveAll<IAdminSeoService>(); services.AddSingleton<IAdminSeoService>(service); }
            });
        });

    private static Dictionary<string, string?> JwtSettings() => new() {
        ["Authentication:Jwt:Issuer"] = "https://issuer.example",
        ["Authentication:Jwt:Audience"] = "ekiphan-admin",
        ["Authentication:Jwt:SigningKey"] = SigningKey };

    private sealed class StubService : IAdminSeoService
    {
        public AdminSeoQuery? Query { get; private set; }
        public AdminSeoAccessScope? Scope { get; private set; }
        public Task<AdminSeoPage> GetPageAsync(AdminSeoQuery query, AdminSeoAccessScope scope, CancellationToken cancellationToken = default) { Query = query; Scope = scope; return Task.FromResult(new AdminSeoPage([], query.Page, query.PageSize, 0)); }
        public Task<AdminSeoHealth> GetHealthAsync(AdminSeoAccessScope scope, CancellationToken cancellationToken = default) => Task.FromResult(new AdminSeoHealth(0,0,0,0,0,0,0,0,[]));
        public Task<IReadOnlyList<AdminSeoDuplicate>> GetDuplicatesAsync(AdminSeoAccessScope scope, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AdminSeoDuplicate>>([]);
        public Task<AdminSeoDetail?> GetDetailAsync(SeoContentType type, Guid id, AdminSeoAccessScope scope, CancellationToken cancellationToken = default) => Task.FromResult<AdminSeoDetail?>(null);
        public Task<AdminSeoDetail?> UpdateAsync(SeoContentType type, Guid id, UpdateAdminSeoCommand command, AdminSeoAccessScope scope, CancellationToken cancellationToken = default) => Task.FromResult<AdminSeoDetail?>(null);
        public Task<SlugAvailability> IsSlugAvailableAsync(SeoContentType type, Guid? id, string language, string slug, AdminSeoAccessScope scope, CancellationToken cancellationToken = default) => Task.FromResult(new SlugAvailability(true));
    }
}
