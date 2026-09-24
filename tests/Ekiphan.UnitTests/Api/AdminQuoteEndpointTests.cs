using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Ekiphan.Application.Quotes;
using Ekiphan.Application.Identity;
using Ekiphan.Domain.Quotes;
using Ekiphan.UnitTests.Quotes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ekiphan.UnitTests.Api;

public sealed class AdminQuoteEndpointTests
{
    private const string SigningKey =
        "test-only-signing-key-at-least-32-bytes";

    [Fact]
    public async Task AdminQuotesFailClosedWithoutJwtConfiguration()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/admin/quotes");

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
    }

    [Fact]
    public async Task AdminQuotesRequireAuthentication()
    {
        await using var factory = CreateFactory(JwtSettings());
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/admin/quotes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminQuotesRequireQuotePermission()
    {
        await using var factory = CreateFactory(JwtSettings());
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateAccessToken("imports.manage"));

        using var response = await client.GetAsync("/api/admin/quotes");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizedListBindsFiltersAndDisablesCaching()
    {
        var queryService = new StubQueryService();
        await using var factory = CreateFactory(
            JwtSettings(),
            services =>
            {
                services.RemoveAll<IAdminQuoteQueryService>();
                services.AddSingleton<IAdminQuoteQueryService>(queryService);
            });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateAccessToken("quotes.read"));

        using var response = await client.GetAsync(
            "/api/admin/quotes?page=2&pageSize=10" +
            "&status=Reviewing&search=hotel");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, queryService.Page);
        Assert.Equal(10, queryService.PageSize);
        Assert.Equal(QuoteStatus.Reviewing, queryService.Status);
        Assert.Equal("hotel", queryService.Search);
        Assert.Contains(
            "no-store",
            response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task QuoteReaderCannotChangeStatus()
    {
        var quote = QuoteManagementServiceTests.CreateQuote(
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        await using var factory = CreateFactory(JwtSettings(), services =>
        {
            services.RemoveAll<IQuoteManagementRepository>();
            services.AddSingleton<IQuoteManagementRepository>(
                new QuoteManagementServiceTests.StubRepository(quote));
        });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateAccessToken("quotes.read"));

        using var response = await client.PostAsJsonAsync(
            $"/api/admin/quotes/{quote.Id}/status",
            new
            {
                status = QuoteStatus.Reviewing,
                expectedVersion = Convert.ToBase64String(quote.RowVersion),
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizedStatusChangeRecordsAuthenticatedUser()
    {
        var version = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var quote = QuoteManagementServiceTests.CreateQuote(version);
        var repository =
            new QuoteManagementServiceTests.StubRepository(quote);
        var userId = Guid.NewGuid();
        await using var factory = CreateFactory(
            JwtSettings(),
            services =>
            {
                services.RemoveAll<IQuoteManagementRepository>();
                services.AddSingleton<IQuoteManagementRepository>(repository);
            });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateAccessToken("quotes.manage", userId));

        using var response = await client.PostAsJsonAsync(
            $"/api/admin/quotes/{quote.Id}/status",
            new
            {
                status = QuoteStatus.Reviewing,
                expectedVersion = Convert.ToBase64String(version),
                note = "Assigned.",
            });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(userId, quote.StatusHistory.Last().ChangedByUserId);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(
            (int)QuoteStatus.Reviewing,
            json.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task StaleStatusChangeReturnsConflict()
    {
        var quote = QuoteManagementServiceTests.CreateQuote(
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        await using var factory = CreateFactory(
            JwtSettings(),
            services =>
            {
                services.RemoveAll<IQuoteManagementRepository>();
                services.AddSingleton<IQuoteManagementRepository>(
                    new QuoteManagementServiceTests.StubRepository(quote));
            });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateAccessToken("quotes.manage"));

        using var response = await client.PostAsJsonAsync(
            $"/api/admin/quotes/{quote.Id}/status",
            new
            {
                status = QuoteStatus.Reviewing,
                expectedVersion = Convert.ToBase64String(
                    new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 }),
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
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
                        "Database=AdminQuoteEndpointTests;" +
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

    private static string CreateAccessToken(
        string permission,
        Guid? userId = null)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = "https://issuer.example",
            Audience = "ekiphan-admin",
            Expires = DateTime.UtcNow.AddMinutes(5),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = (userId ?? Guid.NewGuid()).ToString(),
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

    private sealed class StubQueryService : IAdminQuoteQueryService
    {
        public int Page { get; private set; }

        public int PageSize { get; private set; }

        public QuoteStatus? Status { get; private set; }

        public string? Search { get; private set; }

        public Task<AdminQuotePagedResult<AdminQuoteSummary>> GetQuotesAsync(
            AdminQuoteListQuery query,
            CancellationToken cancellationToken = default)
        {
            Page = query.Page;
            PageSize = query.PageSize;
            Status = query.Status;
            Search = query.Search;
            return Task.FromResult(
                new AdminQuotePagedResult<AdminQuoteSummary>(
                    [],
                    query.Page,
                    query.PageSize,
                    0));
        }

        public Task<AdminQuoteDetail?> GetQuoteAsync(
            Guid quoteId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AdminQuoteDetail?>(null);

        public Task<IReadOnlyList<AdminRequestAssignee>> GetAssigneesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AdminRequestAssignee>>([]);
    }
}
