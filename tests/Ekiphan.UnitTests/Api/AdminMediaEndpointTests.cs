using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Ekiphan.Application.Media;
using Ekiphan.Application.Identity;
using Ekiphan.Domain.Media;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ekiphan.UnitTests.Api;

public sealed class AdminMediaEndpointTests
{
    private const string SigningKey =
        "test-only-signing-key-at-least-32-bytes";

    [Fact]
    public async Task UploadFailsClosedWithoutJwtConfiguration()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var request = CreateImageRequest();

        using var response = await client.PostAsync(
            "/api/admin/media",
            request);

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
    }

    [Fact]
    public async Task UploadRequiresAuthenticationAndMediaPermission()
    {
        await using var factory = CreateFactory(JwtSettings());
        using var client = factory.CreateClient();
        using var anonymousRequest = CreateImageRequest();

        using var anonymousResponse = await client.PostAsync(
            "/api/admin/media",
            anonymousRequest);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            anonymousResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateAccessToken("catalog.manage"));
        using var forbiddenRequest = CreateImageRequest();
        using var forbiddenResponse = await client.PostAsync(
            "/api/admin/media",
            forbiddenRequest);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            forbiddenResponse.StatusCode);
    }

    [Fact]
    public async Task ValidAuthorizedUploadReturnsCreatedAndNoStore()
    {
        var repository = new StubRepository();
        var service = new MediaUploadService(
            new MediaFileSignatureValidator(),
            new StubScanner(MediaThreatScanStatus.Clean),
            new StubStorage(),
            repository,
            TimeProvider.System);
        await using var factory = CreateFactory(
            JwtSettings(),
            services =>
            {
                services.RemoveAll<MediaUploadService>();
                services.AddSingleton(service);
            });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateAccessToken("media.manage"));
        using var request = CreateImageRequest();

        using var response = await client.PostAsync(
            "/api/admin/media",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(repository.Asset);
        Assert.Contains(
            "no-store",
            response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task ScannerUnavailableReturnsServiceUnavailable()
    {
        var service = new MediaUploadService(
            new MediaFileSignatureValidator(),
            new StubScanner(MediaThreatScanStatus.Unavailable),
            new StubStorage(),
            new StubRepository(),
            TimeProvider.System);
        await using var factory = CreateFactory(
            JwtSettings(),
            services =>
            {
                services.RemoveAll<MediaUploadService>();
                services.AddSingleton(service);
            });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateAccessToken("media.manage"));
        using var request = CreateImageRequest();

        using var response = await client.PostAsync(
            "/api/admin/media",
            request);

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
    }

    [Fact]
    public async Task LibraryBindsServerSidePagingAndFilters()
    {
        var media = new StubAdminMediaService();
        await using var factory = CreateFactory(
            JwtSettings(),
            services =>
            {
                services.RemoveAll<IAdminMediaService>();
                services.AddSingleton<IAdminMediaService>(media);
            });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateAccessToken("media.manage"));

        using var response = await client.GetAsync(
            "/api/admin/media-library?page=2&pageSize=10" +
            "&search=plate&assetType=Image&status=Active");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(("plate", "Image", "Active", 2, 10), media.Query);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
    }

    private static MultipartFormDataContent CreateImageRequest()
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xE0]);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(file, "file", "product.jpg");
        form.Add(new StringContent("Image"), "assetType");
        form.Add(new StringContent("tr"), "languageCode");
        form.Add(new StringContent("Ürün görseli"), "title");
        form.Add(new StringContent("Beyaz servis tabağı"), "altText");
        return form;
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
                        "Database=AdminMediaEndpointTests;" +
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

    private sealed class StubScanner(MediaThreatScanStatus status)
        : IMediaThreatScanner
    {
        public Task<MediaThreatScanStatus> ScanAsync(
            Stream content,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(status);
    }

    private sealed class StubStorage : IMediaFileStorage
    {
        public bool IsConfigured => true;

        public Task SaveAsync(
            string storageKey,
            Stream content,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(
            string storageKey,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubRepository : IMediaAssetRepository
    {
        public MediaAsset? Asset { get; private set; }

        public Task AddAsync(
            MediaAsset asset,
            CancellationToken cancellationToken = default)
        {
            Asset = asset;
            return Task.CompletedTask;
        }
    }

    private sealed class StubAdminMediaService : IAdminMediaService
    {
        public (string?, string?, string?, int, int) Query { get; private set; }

        public Task<AdminMediaLibrary> GetAsync(
            string? search,
            string? assetType,
            string? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            Query = (search, assetType, status, page, pageSize);
            return Task.FromResult(new AdminMediaLibrary(
                [], [], [], [], [], page, pageSize, 0));
        }

        public Task<AdminMediaAssetDetail> CreateExternalVideoAsync(
            CreateExternalVideoCommand command,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AdminMediaAssetDetail?> UpdateAssetAsync(
            Guid mediaAssetId,
            SaveAdminMediaCommand command,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AdminMediaAssignmentDetail> SaveAssignmentAsync(
            SaveMediaAssignmentCommand command,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> RemoveAssignmentAsync(
            string targetType,
            Guid targetId,
            Guid mediaAssetId,
            string role,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
