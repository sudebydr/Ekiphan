using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Ekiphan.Application.Administration;
using Ekiphan.Application.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ekiphan.UnitTests.Api;

public sealed class AdminDashboardEndpointTests
{
    private const string SigningKey =
        "test-only-signing-key-at-least-32-bytes";

    [Fact]
    public async Task EndpointFailsClosedWithoutJwtConfiguration()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder => builder.UseSetting(
                    "ConnectionStrings:EkiphanDatabase",
                    "Server=(localdb)\\mssqllocaldb;" +
                    "Database=AdminDashboardEndpointTests;" +
                    "Trusted_Connection=True"));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/admin/dashboard");

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
    }

    [Fact]
    public async Task EndpointRequiresAuthentication()
    {
        await using var factory = CreateFactory(new StubDashboardService());
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/admin/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EndpointRejectsUserWithoutDashboardPermission()
    {
        await using var factory = CreateFactory(new StubDashboardService());
        using var client = factory.CreateClient();
        Authorize(client, "unknown.manage");

        using var response = await client.GetAsync("/api/admin/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EndpointReturnsSuccessfulSafeSummary()
    {
        var service = new StubDashboardService(Summary());
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();
        Authorize(client, "catalog.manage");

        using var response = await client.GetAsync("/api/admin/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        Assert.Equal(
            12,
            document.RootElement
                .GetProperty("products")
                .GetProperty("total")
                .GetInt32());
        Assert.Equal(
            "SKU-1",
            document.RootElement
                .GetProperty("recentProducts")[0]
                .GetProperty("sku")
                .GetString());
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("accessToken", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("phone", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", json, StringComparison.OrdinalIgnoreCase);
        Assert.True(document.RootElement
            .GetProperty("system")
            .GetProperty("apiOperational")
            .GetBoolean());
        Assert.True(document.RootElement
            .GetProperty("system")
            .GetProperty("databaseReachable")
            .GetBoolean());
    }

    [Fact]
    public async Task EndpointReturnsEmptySummaryWithoutFailure()
    {
        var service = new StubDashboardService(EmptySummary());
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();
        Authorize(client, "catalog.manage");

        using var response = await client.GetAsync("/api/admin/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        Assert.Equal(
            0,
            document.RootElement
                .GetProperty("products")
                .GetProperty("total")
                .GetInt32());
        Assert.Equal(
            0,
            document.RootElement
                .GetProperty("recentProducts")
                .GetArrayLength());
    }

    [Fact]
    public async Task EndpointPassesOnlyGrantedDataScopesToQueryService()
    {
        var service = new StubDashboardService();
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();
        Authorize(client, "media.manage");

        using var response = await client.GetAsync("/api/admin/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(service.Access);
        Assert.True(service.Access.Media);
        Assert.False(service.Access.Catalog);
        Assert.False(service.Access.Quotes);
        Assert.False(service.Access.Imports);
        Assert.False(service.Access.Content);
    }

    [Fact]
    public void DashboardAccessMapsPublishPermissionToImportScope()
    {
        var access = AdminDashboardAccess.FromPermissions(
            ["imports.publish", "content.manage"]);

        Assert.True(access.Imports);
        Assert.True(access.Content);
        Assert.False(access.Catalog);
        Assert.False(access.Quotes);
        Assert.False(access.Media);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IAdminDashboardService dashboardService) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder =>
                {
                    builder.UseSetting(
                        "ConnectionStrings:EkiphanDatabase",
                        "Server=(localdb)\\mssqllocaldb;" +
                        "Database=AdminDashboardEndpointTests;" +
                        "Trusted_Connection=True");
                    builder.UseSetting(
                        "Authentication:Jwt:Issuer",
                        "https://issuer.example");
                    builder.UseSetting(
                        "Authentication:Jwt:Audience",
                        "ekiphan-admin");
                    builder.UseSetting(
                        "Authentication:Jwt:SigningKey",
                        SigningKey);
                    builder.ConfigureTestServices(
                        services =>
                        {
                            services.RemoveAll<IAdminAuthenticationService>();
                            services.AddSingleton<IAdminAuthenticationService,
                                TestAdminAuthenticationService>();
                            services.RemoveAll<IAdminDashboardService>();
                            services.AddSingleton(dashboardService);
                        });
                });

    private static void Authorize(HttpClient client, string permission) =>
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateAccessToken(permission));

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
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static AdminDashboard Summary()
    {
        var now = DateTimeOffset.UtcNow;
        return new AdminDashboard(
            GeneratedAt: now,
            Products: new AdminProductMetrics(12, 8, 4, 2, 0, 3),
            TotalCategories: 6,
            TotalBrands: 5,
            Quotes: new AdminQuoteMetrics(2, 3, 5),
            NewContactRequests: 7,
            TotalMediaFiles: 18,
            ContentUpdatesLast30Days: 4,
            RecentQuotes: [new AdminDashboardQuote(Guid.NewGuid(), "TKL-1", "Test Şirketi", "New", 2, now)],
            RecentProducts: [new AdminDashboardProduct(Guid.NewGuid(), "SKU-1", "Ürün", true, now)],
            ProductsMissingImages: [],
            ProductsMissingEnglishContent: [],
            LatestImport: null,
            System: new AdminDashboardSystemStatus(true, true, 3),
            Warnings: []);
    }

    private static AdminDashboard EmptySummary()
    {
        var now = DateTimeOffset.UtcNow;
        return new AdminDashboard(
            GeneratedAt: now,
            Products: new AdminProductMetrics(0, 0, 0, 0, 0, 0),
            TotalCategories: 0,
            TotalBrands: 0,
            Quotes: null,
            NewContactRequests: null,
            TotalMediaFiles: null,
            ContentUpdatesLast30Days: null,
            RecentQuotes: null,
            RecentProducts: [],
            ProductsMissingImages: [],
            ProductsMissingEnglishContent: [],
            LatestImport: null,
            System: new AdminDashboardSystemStatus(true, true, 3),
            Warnings: []);
    }

    private sealed class StubDashboardService(
        AdminDashboard? result = null) : IAdminDashboardService
    {
        public AdminDashboardAccess? Access { get; private set; }

        public Task<AdminDashboard> GetAsync(
            AdminDashboardAccess access,
            CancellationToken cancellationToken = default)
        {
            Access = access;
            return Task.FromResult(result ?? EmptySummary());
        }
    }
}
