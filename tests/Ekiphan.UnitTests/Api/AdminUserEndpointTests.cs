using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Ekiphan.Application.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ekiphan.UnitTests.Api;

public sealed class AdminUserEndpointTests
{
    private const string SigningKey =
        "test-only-signing-key-at-least-32-bytes";
    private static readonly string[] UserManagePermission =
        ["users.manage"];
    private static readonly string[] UserAndCatalogPermissions =
        ["users.manage", "catalog.manage"];

    [Fact]
    public async Task EndpointsFailClosedWithoutJwtConfiguration()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
    }

    [Fact]
    public async Task AuthorizedListBindsPagingAndReturnsPermissions()
    {
        var service = new StubService
        {
            PageResult = new AdminUserPage(
                [
                    Summary(
                        "alpha@example.com",
                        ["catalog.manage", "users.manage"]),
                    Summary("beta@example.com", ["users.manage"]),
                ],
                2,
                10,
                12),
        };
        await using var factory = CreateFactory(service, JwtSettings());
        using var client = AuthorizedClient(factory);

        using var response = await client.GetAsync(
            "/api/admin/users?page=2&pageSize=10&search=alpha");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            new AdminUserListQuery(2, 10, "alpha"),
            service.ListQuery);
        var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync());
        Assert.Equal(
            2,
            document.RootElement.GetProperty("items").GetArrayLength());
        Assert.Equal(
            2,
            document.RootElement.GetProperty("items")[0]
                .GetProperty("permissions")
                .GetArrayLength());
        Assert.Equal(
            12,
            document.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Contains(
            "no-store",
            response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task AuthorizedListReturnsEmptyPage()
    {
        var service = new StubService();
        await using var factory = CreateFactory(service, JwtSettings());
        using var client = AuthorizedClient(factory);

        using var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<AdminUserPage>();
        Assert.NotNull(page);
        Assert.Empty(page.Items);
        Assert.Equal(1, page.Page);
        Assert.Equal(50, page.PageSize);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?page=invalid")]
    public async Task ListRejectsInvalidPaging(string query)
    {
        var service = new StubService();
        await using var factory = CreateFactory(service, JwtSettings());
        using var client = AuthorizedClient(factory);

        using var response = await client.GetAsync(
            $"/api/admin/users{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(service.ListQuery);
    }

    [Fact]
    public async Task SuccessfulCreateReturnsCreatedSummary()
    {
        var service = new StubService();
        await using var factory = CreateFactory(service, JwtSettings());
        using var client = AuthorizedClient(factory);

        using var response = await client.PostAsJsonAsync(
            "/api/admin/users",
            new
            {
                email = "created@example.com",
                displayName = "Created User",
                password = "Strong-Test-Password-1!",
                isActive = true,
                permissions = UserAndCatalogPermissions,
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(service.Created);
        Assert.Equal("created@example.com", service.Created.Email);
        var user = await response.Content.ReadFromJsonAsync<AdminUserSummary>();
        Assert.NotNull(user);
        Assert.Equal("created@example.com", user.Email);
        Assert.Equal(2, user.Permissions.Count);
    }

    [Fact]
    public async Task ValidationFailureReturnsBadRequest()
    {
        var service = new StubService
        {
            CreateException = new ArgumentException("Invalid user."),
        };
        await using var factory = CreateFactory(service, JwtSettings());
        using var client = AuthorizedClient(factory);

        using var response = await client.PostAsJsonAsync(
            "/api/admin/users",
            new
            {
                email = "invalid",
                displayName = "Invalid User",
                password = "weak",
                isActive = true,
                permissions = UserManagePermission,
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DuplicateEmailReturnsConflict()
    {
        var service = new StubService
        {
            CreateException = new AdminUserConflictException(
                "The admin email is already in use."),
        };
        await using var factory = CreateFactory(service, JwtSettings());
        using var client = AuthorizedClient(factory);

        using var response = await client.PostAsJsonAsync(
            "/api/admin/users",
            new
            {
                email = "duplicate@example.com",
                displayName = "Duplicate User",
                password = "Strong-Test-Password-1!",
                isActive = true,
                permissions = UserManagePermission,
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static HttpClient AuthorizedClient(
        WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateAccessToken());
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
                        "Database=AdminUserEndpointTests;" +
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
                                services.RemoveAll<
                                    IAdminUserManagementService>();
                                services.AddSingleton<
                                    IAdminUserManagementService>(service);
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
                ["permission"] = "users.manage",
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static AdminUserSummary Summary(
        string email,
        IReadOnlyList<string> permissions) =>
        new(
            Guid.NewGuid(),
            email,
            email,
            true,
            0,
            null,
            null,
            permissions,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

    private sealed class StubService : IAdminUserManagementService
    {
        public AdminUserPage PageResult { get; init; } =
            new([], 1, 50, 0);

        public Exception? CreateException { get; init; }

        public AdminUserListQuery? ListQuery { get; private set; }

        public CreateAdminUserCommand? Created { get; private set; }

        public Task<AdminUserPage> GetUsersAsync(
            AdminUserListQuery query,
            CancellationToken cancellationToken = default)
        {
            ListQuery = query;
            return Task.FromResult(
                PageResult with
                {
                    Page = query.Page,
                    PageSize = query.PageSize,
                });
        }

        public Task<AdminUserSummary> CreateAsync(
            CreateAdminUserCommand command,
            CancellationToken cancellationToken = default)
        {
            if (CreateException is not null)
            {
                return Task.FromException<AdminUserSummary>(CreateException);
            }

            Created = command;
            return Task.FromResult(
                Summary(command.Email, command.Permissions));
        }

        public Task<AdminUserSummary?> UpdateAsync(
            Guid userId,
            UpdateAdminUserCommand command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AdminUserSummary?>(null);

        public Task<bool> ResetPasswordAsync(
            Guid userId,
            string password,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }
}
