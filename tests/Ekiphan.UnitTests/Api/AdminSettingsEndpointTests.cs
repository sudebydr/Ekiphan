using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Ekiphan.Application.Identity;
using Ekiphan.Application.Settings;
using Ekiphan.Application.Settings.Models;
using Ekiphan.Domain.Settings;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ekiphan.UnitTests.Api;

public sealed class AdminSettingsEndpointTests
{
    private const string SigningKey = "test-only-signing-key-at-least-32-bytes";

    [Fact]
    public async Task AdminGetReturns200WithSettingsManagePermission()
    {
        var stubRepo = new StubRepo();
        var service = new SiteSettingsService(stubRepo);
        
        await using var factory = CreateFactory(service, JwtSettings());
        using var client = AuthorizedClient(factory, "settings.manage");
        
        using var response = await client.GetAsync("/api/admin/settings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminPutReturns200WithSettingsManagePermission()
    {
        var stubRepo = new StubRepo();
        var service = new SiteSettingsService(stubRepo);

        await using var factory = CreateFactory(service, JwtSettings());
        using var client = AuthorizedClient(factory, "settings.manage");
        
        var request = new UpdateSiteSettingsRequest("phone", "fax", "email@a.com", "addr1", "addr2", "addr3", null, null, null, "title", "slogan", null, Convert.ToBase64String(stubRepo.Settings.RowVersion));
        using var response = await client.PutAsJsonAsync("/api/admin/settings", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CatalogManagerCannotAccessSettings()
    {
        await using var factory = CreateFactory(null, JwtSettings());
        using var client = AuthorizedClient(factory, "catalog.manage");
        using var response = await client.GetAsync("/api/admin/settings");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    
    [Fact]
    public async Task UnauthorizedReturns401()
    {
        await using var factory = CreateFactory(null, JwtSettings());
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/admin/settings");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PublicGetReturns200WithoutAuth()
    {
        var stubRepo = new StubRepo();
        var service = new SiteSettingsService(stubRepo);

        await using var factory = CreateFactory(service, JwtSettings());
        using var client = factory.CreateClient();
        
        using var response = await client.GetAsync("/api/settings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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

    private static WebApplicationFactory<Program> CreateFactory(SiteSettingsService? service = null, IReadOnlyDictionary<string, string?>? settings = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:EkiphanDatabase", "Server=(localdb)\\mssqllocaldb;Database=AdminSettingsEndpointTests;Trusted_Connection=True");
            if (settings is not null) foreach (var setting in settings) builder.UseSetting(setting.Key, setting.Value);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAdminAuthenticationService>();
                services.AddSingleton<IAdminAuthenticationService, TestAdminAuthenticationService>();
                if (service is not null) 
                { 
                    services.RemoveAll<SiteSettingsService>(); 
                    services.AddSingleton(service); 
                }
            });
        });

    private static Dictionary<string, string?> JwtSettings() => new() {
        ["Authentication:Jwt:Issuer"] = "https://issuer.example",
        ["Authentication:Jwt:Audience"] = "ekiphan-admin",
        ["Authentication:Jwt:SigningKey"] = SigningKey };

    private sealed class StubRepo : ISiteSettingsRepository
    {
        public SiteSettings Settings = new SiteSettings(Guid.NewGuid(), "phone", "fax", "email@a.com", "addr1", "addr2", "addr3", null, null, null, "title", "slogan", null);
        public Task<SiteSettings> GetSettingsAsync(CancellationToken cancellationToken = default) => Task.FromResult(Settings);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
