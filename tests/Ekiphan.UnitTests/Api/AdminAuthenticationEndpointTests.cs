using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ekiphan.Application.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Ekiphan.UnitTests.Api;

public sealed class AdminAuthenticationEndpointTests
{
    private const string SigningKey =
        "test-only-signing-key-at-least-32-bytes";

    [Fact]
    public async Task LoginFailsClosedWithoutJwtConfiguration()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder => builder.UseSetting(
                    "ConnectionStrings:EkiphanDatabase",
                    "Server=(localdb)\\mssqllocaldb;" +
                    "Database=AdminAuthenticationEndpointTests;" +
                    "Trusted_Connection=True"));
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/admin/auth/login",
            new { email = "admin@example.com", password = "not-a-secret" });

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
    }

    [Fact]
    public async Task SessionFailsClosedWithoutJwtConfiguration()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder => builder.UseSetting(
                    "ConnectionStrings:EkiphanDatabase",
                    "Server=(localdb)\\mssqllocaldb;" +
                    "Database=AdminAuthenticationEndpointTests;" +
                    "Trusted_Connection=True"));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/admin/auth/session");

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
    }

    [Fact]
    public async Task ValidCredentialsReturnShortLivedTokenWithPermissions()
    {
        await using var factory = CreateFactory(
            new StubAuthenticationService());
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/admin/auth/login",
            new
            {
                email = "admin@example.com",
                password = "Strong-Test-Password-1!",
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync());
        var token = document.RootElement
            .GetProperty("accessToken")
            .GetString();
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);
        Assert.Equal("https://issuer.example", jwt.Issuer);
        Assert.Contains(
            jwt.Claims,
            claim =>
                claim.Type == "permission" &&
                claim.Value.Contains("catalog.manage"));
    }

    [Fact]
    public async Task SessionRequiresBearerToken()
    {
        await using var factory = CreateFactory(
            new StubAuthenticationService());
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/admin/auth/session");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidSessionReturnsSafeCurrentAdmin()
    {
        var service = new StubAuthenticationService();
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                await LoginAsync(client));

        using var response = await client.GetAsync(
            "/api/admin/auth/session");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync());
        Assert.True(
            document.RootElement.GetProperty("authenticated").GetBoolean());
        Assert.Equal(
            30,
            document.RootElement
                .GetProperty("idleTimeoutMinutes")
                .GetInt32());
        var user = document.RootElement.GetProperty("user");
        Assert.Equal(service.UserId.ToString(), user.GetProperty("id").GetString());
        Assert.Equal(
            "admin@example.com",
            user.GetProperty("email").GetString());
        Assert.Equal(
            "Administrator",
            user.GetProperty("displayName").GetString());
        Assert.Equal(0, user.GetProperty("roles").GetArrayLength());
        Assert.Contains(
            user.GetProperty("permissions").EnumerateArray(),
            item => item.GetString() == "catalog.manage");
        Assert.False(user.TryGetProperty("securityStamp", out _));
        Assert.False(user.TryGetProperty("password", out _));
        Assert.False(document.RootElement.TryGetProperty("accessToken", out _));
    }

    [Fact]
    public async Task SessionRejectsInactiveAdmin()
    {
        var service = new StubAuthenticationService();
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();
        var token = await LoginAsync(client);
        service.IsActive = false;
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync(
            "/api/admin/auth/session");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SessionRejectsChangedSecurityStamp()
    {
        var service = new StubAuthenticationService();
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();
        var token = await LoginAsync(client);
        service.RotateSecurityStamp();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync(
            "/api/admin/auth/session");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SessionRejectsAdminWithoutPermission()
    {
        var service = new StubAuthenticationService([]);
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                await LoginAsync(client));

        using var response = await client.GetAsync(
            "/api/admin/auth/session");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task LogoutRevokesBackendSession()
    {
        var service = new StubAuthenticationService();
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var logout = await client.PostAsync(
            "/api/admin/auth/logout",
            null);
        using var session = await client.GetAsync(
            "/api/admin/auth/session");

        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, session.StatusCode);
    }

    [Fact]
    public async Task ExpiredIdleSessionIsRejected()
    {
        var service = new StubAuthenticationService();
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();
        var token = await LoginAsync(client);
        service.IsIdleExpired = true;
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync(
            "/api/admin/auth/session");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IAdminAuthenticationService authenticationService) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder =>
                {
                    builder.UseSetting(
                        "ConnectionStrings:EkiphanDatabase",
                        "Server=(localdb)\\mssqllocaldb;" +
                        "Database=AdminAuthenticationEndpointTests;" +
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
                    builder.UseSetting("Authentication:Jwt:IdleTimeoutMinutes", "30");
                    builder.ConfigureTestServices(
                        services =>
                        {
                            services.RemoveAll<IAdminAuthenticationService>();
                            services.AddSingleton(authenticationService);
                        });
                });

    private static async Task<string> LoginAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/admin/auth/login",
            new
            {
                email = "admin@example.com",
                password = "Strong-Test-Password-1!",
            });
        response.EnsureSuccessStatusCode();
        var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync());
        return document.RootElement
            .GetProperty("accessToken")
            .GetString()!;
    }

    private sealed class StubAuthenticationService
        : IAdminAuthenticationService
    {
        private string securityStamp = Guid.NewGuid().ToString("N");
        private readonly IReadOnlyList<string> permissions;
        private readonly HashSet<Guid> sessions = [];

        public StubAuthenticationService(
            IReadOnlyList<string>? permissions = null)
        {
            this.permissions = permissions ??
                ["catalog.manage", "quotes.manage"];
        }

        public Guid UserId { get; } = Guid.NewGuid();

        public bool IsActive { get; set; } = true;

        public bool IsIdleExpired { get; set; }

        public Task<AdminAuthenticatedUser?> AuthenticateAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AdminAuthenticatedUser?>(
                new AdminAuthenticatedUser(
                    UserId,
                    email,
                    "Administrator",
                    permissions,
                    securityStamp));

        public Task CreateSessionAsync(
            Guid userId,
            Guid sessionId,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken = default)
        {
            if (userId != UserId || expiresAt <= DateTimeOffset.UtcNow)
            {
                throw new InvalidOperationException();
            }

            sessions.Add(sessionId);
            return Task.CompletedTask;
        }

        public Task<AdminAuthenticatedUser?> ValidateSessionAsync(
            Guid userId,
            Guid sessionId,
            string currentSecurityStamp,
            TimeSpan idleTimeout,
            CancellationToken cancellationToken = default)
        {
            if (!IsActive ||
                IsIdleExpired ||
                userId != UserId ||
                !sessions.Contains(sessionId) ||
                idleTimeout != TimeSpan.FromMinutes(30) ||
                !string.Equals(
                    currentSecurityStamp,
                    securityStamp,
                    StringComparison.Ordinal))
            {
                return Task.FromResult<AdminAuthenticatedUser?>(null);
            }

            return Task.FromResult<AdminAuthenticatedUser?>(
                new AdminAuthenticatedUser(
                    UserId,
                    "admin@example.com",
                    "Administrator",
                    permissions,
                    securityStamp));
        }

        public Task<bool> RevokeSessionAsync(
            Guid userId,
            Guid sessionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                userId == UserId && sessions.Remove(sessionId));

        public void RotateSecurityStamp()
        {
            securityStamp = Guid.NewGuid().ToString("N");
        }
    }
}
