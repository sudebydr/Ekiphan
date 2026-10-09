using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using Ekiphan.Api.Media;
using Ekiphan.Application.CatalogPdfImport;
using Ekiphan.Application.Media;
using Ekiphan.Application.MediaImport;
using Ekiphan.Domain.Media;
using Ekiphan.Infrastructure.Media;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ekiphan.UnitTests.Api;

public sealed class ZipUploadEndpointTests
{
    [Theory]
    [InlineData("catalog")]
    [InlineData("pdf")]
    [InlineData("image")]
    [InlineData("image", "missing")]
    [InlineData("image", "ambiguous")]
    [InlineData("image", "duplicate")]
    public async Task ChunkUploadAndRepeatedProcessingRequestsCallExistingServicesExactlyOnce(string kind, string scenario = "matched")
    {
        var root = Path.Combine(Path.GetTempPath(), "ekiphan-zip-http-test-" + Guid.NewGuid().ToString("N"));
        var service = new CatalogStub();
        var assets = new Assets();
        var products = new Products(scenario);
        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("ConnectionStrings:EkiphanDatabase", "Server=invalid.local;Database=ZipUploadSyntheticTests;Integrated Security=True");
                builder.UseSetting("MediaStorage:Provider", "Local");
                builder.UseSetting("Authentication:Jwt:Issuer", "https://test.example");
                builder.UseSetting("Authentication:Jwt:Audience", "test");
                builder.UseSetting("Authentication:Jwt:SigningKey", "Synthetic-zip-test-signing-key-at-least-32-characters");
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IHostedService>();
                    services.RemoveAll<ZipUploadStore>();
                    services.AddSingleton(_ => new ZipUploadStore(new() { Root = root }));
                    services.AddHostedService(provider => provider.GetRequiredService<ZipUploadWorker>());
                    services.RemoveAll<ICatalogPdfImportService>();
                    services.AddSingleton<ICatalogPdfImportService>(service);
                    services.RemoveAll<IProductMediaImportRepository>(); services.AddSingleton<IProductMediaImportRepository>(products);
                    services.RemoveAll<IMediaAssetRepository>(); services.AddSingleton<IMediaAssetRepository>(assets);
                    services.RemoveAll<IMediaThreatScanner>(); services.AddSingleton<IMediaThreatScanner>(new Scanner());
                    services.RemoveAll<IMediaFileStorage>(); services.AddSingleton<IMediaFileStorage>(new Storage());
                    services.AddAuthentication(options =>
                    { options.DefaultAuthenticateScheme = "ZipTest"; options.DefaultChallengeScheme = "ZipTest"; })
                        .AddScheme<AuthenticationSchemeOptions, ZipAuthentication>("ZipTest", _ => { });
                    services.AddAuthorization(options => { options.AddPolicy("MediaImport", policy => policy.RequireAuthenticatedUser());
                        options.AddPolicy("MediaManage", policy => policy.RequireAuthenticatedUser()); });
                });
            });
            using var client = factory.CreateClient();
            var bytes = kind == "image" ? Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScL0WQAAAABJRU5ErkJggg==") : new byte[] { 1, 2, 3, 4 };
            using var create = await client.PostAsJsonAsync("/api/admin/zip-uploads", new
            { kind, fileName = kind == "image" ? "4160.BRD.02TP02.png" : kind == "pdf" ? "test.pdf" : "test.zip", length = bytes.Length, fingerprint = new string('A', 64) });
            Assert.Equal(HttpStatusCode.OK, create.StatusCode);
            var state = (await create.Content.ReadFromJsonAsync<ZipUploadState>())!;
            using var part = new ByteArrayContent(bytes);
            part.Headers.Add("X-Chunk-SHA256", Convert.ToHexString(SHA256.HashData(bytes)));
            using var put = await client.PutAsync($"/api/admin/zip-uploads/{state.Id}?offset=0", part);
            Assert.Equal(HttpStatusCode.OK, put.StatusCode);
            foreach (var operation in kind == "catalog" ? new[] { "preview", "execute" } : new[] { "execute" })
            {
                var path = $"/api/admin/zip-uploads/{state.Id}/process";
                using var first = await client.PostAsJsonAsync(path, new { operation, confirmed = true, fields = kind is "image" or "pdf" ? null : new { title = "Synthetic title", languageCode = "tr", altText = "Alt", description = "Description" } });
                using var replay = await client.PostAsJsonAsync(path, new { operation, confirmed = true });
                Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
                Assert.Equal(HttpStatusCode.Accepted, replay.StatusCode);
                for (var attempt = 0; attempt < 100; attempt++)
                {
                    state = (await client.GetFromJsonAsync<ZipUploadState>($"/api/admin/zip-uploads/{state.Id}"))!;
                    if (state.Phase != "processing") break;
                    await Task.Delay(10);
                }
                if (scenario is "missing" or "ambiguous")
                {
                    Assert.Equal("failed", state.Phase);
                    Assert.Contains(scenario == "missing" ? "ürün bulunamadı" : "birden fazla ürün", state.Error);
                    Assert.Empty(assets.Values); Assert.Empty(products.Links);
                    return;
                }
                Assert.Equal("completed", state.Phase);
                using var afterCompletion = await client.PostAsJsonAsync(path, new { operation, confirmed = true });
                Assert.Equal(HttpStatusCode.Accepted, afterCompletion.StatusCode);
            }
            Assert.Equal(kind == "catalog" ? 1 : 0, service.PreviewCalls);
            Assert.Equal(kind == "catalog" ? 1 : 0, service.ExecuteCalls);
            Assert.Equal(kind == "pdf" ? 1 : 0, service.SingleCalls);
            Assert.Equal(kind == "image" && scenario == "matched" ? 1 : 0, assets.Values.Count);
            if (kind == "image" && scenario == "matched") { Assert.Equal(products.Product.Id, Assert.Single(products.Links).Product); Assert.Equal(Assert.Single(assets.Values).Id, products.Links[0].Asset); }
            if (kind == "image") {
                Assert.Equal(scenario == "duplicate" ? 0 : 1, state.Result!.Value.GetProperty("importedFiles").GetInt32());
                Assert.Equal(scenario == "duplicate" ? 1 : 0, state.Result!.Value.GetProperty("skippedFiles").GetInt32());
                Assert.Equal(0, state.Result!.Value.GetProperty("errorFiles").GetInt32());
            }
            Assert.Empty((await client.GetFromJsonAsync<ZipUploadSummary[]>("/api/admin/zip-uploads"))!);
            using var close = await client.DeleteAsync($"/api/admin/zip-uploads/{state.Id}");
            Assert.Equal(HttpStatusCode.NoContent, close.StatusCode);
            using var repeatAfterClose = await client.PostAsJsonAsync($"/api/admin/zip-uploads/{state.Id}/process", new { operation = "execute", confirmed = true });
            Assert.Equal(HttpStatusCode.Accepted, repeatAfterClose.StatusCode);
            Assert.Equal(kind == "catalog" ? 1 : 0, service.ExecuteCalls);
            Assert.Equal(kind == "pdf" ? 1 : 0, service.SingleCalls);
            Assert.Equal(kind == "image" && scenario == "matched" ? 1 : 0, assets.Values.Count);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private sealed class ZipAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, "11111111-1111-1111-1111-111111111111")], "ZipTest")), "ZipTest")));
    }

    private sealed class Products(string scenario) : IProductMediaImportRepository
    {
        public ProductMediaProductMatch Product { get; } = new(Guid.NewGuid(), "4160.BRD.02TP02", "Test product", false, true, DateTimeOffset.UtcNow, false);
        public List<(Guid Product, Guid Asset)> Links { get; } = [];
        private ProductMediaImportBatch? batch;
        public Task AddBatchAsync(ProductMediaImportBatch value, CancellationToken cancellationToken = default) { batch = value; return Task.CompletedTask; }
        public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<ProductMediaProductMatch>> FindProductsAsync(IReadOnlyCollection<string> skus, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ProductMediaProductMatch>>(scenario == "missing" ? [] : scenario == "ambiguous" ? [Product, Product with { Id = Guid.NewGuid() }] : skus.Contains(Product.Sku) ? [Product] : []);
        public Task<IReadOnlyDictionary<string, Guid>> FindMediaAssetsByHashesAsync(IReadOnlyCollection<string> hashes, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<string, Guid>>(new Dictionary<string, Guid>());
        public Task<IReadOnlySet<ProductMediaContentLink>> FindProductMediaContentLinksAsync(IReadOnlyCollection<Guid> ids, IReadOnlyCollection<string> hashes, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlySet<ProductMediaContentLink>>(scenario == "duplicate" ? new HashSet<ProductMediaContentLink>(hashes.Select(hash => new ProductMediaContentLink(Product.Id, hash))) : new HashSet<ProductMediaContentLink>());
        public Task<ProductMediaImportBatch?> GetBatchAsync(Guid id, bool tracked, CancellationToken cancellationToken = default) => Task.FromResult(batch);
        public Task<string?> GetProductNameAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<string?>(Product.Name);
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default) => action(cancellationToken);
        public Task AddProductMediaAsync(Guid productId, Guid mediaAssetId, Guid batchId, int sortOrder, bool isPrimary, bool replaceExistingPrimary, DateTimeOffset now, CancellationToken cancellationToken = default) { Links.Add((productId, mediaAssetId)); return Task.CompletedTask; }
        public Task<(bool Removed, bool ArchiveAsset, string? StorageKey)> RollbackItemAsync(ProductMediaImportBatchItem item, DateTimeOffset now, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string?> ArchiveUnassignedMediaAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
    }

    private sealed class Assets : IMediaAssetRepository
    {
        public List<MediaAsset> Values { get; } = [];
        public Task AddAsync(MediaAsset asset, CancellationToken cancellationToken = default)
        { Values.Add(asset); return Task.CompletedTask; }
    }
    private sealed class Scanner : IMediaThreatScanner
    {
        public Task<MediaThreatScanStatus> ScanAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default) =>
            Task.FromResult(MediaThreatScanStatus.Clean);
    }
    private sealed class Storage : IMediaFileStorage
    {
        public bool IsConfigured => true;
        public Task SaveAsync(string key, Stream content, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class CatalogStub : ICatalogPdfImportService
    {
        public int PreviewCalls;
        public int ExecuteCalls;
        public int SingleCalls;
        public Task<CatalogPdfPreview> PreviewAsync(Stream content, string fileName, long length, CancellationToken cancellationToken = default)
        { Interlocked.Increment(ref PreviewCalls); return Task.FromResult(new CatalogPdfPreview(1, 0, [])); }
        public Task<CatalogPdfExecutionResult> ExecuteAsync(Stream content, string fileName, long length,
            IReadOnlyDictionary<string, string>? titles = null, CancellationToken cancellationToken = default)
        { Interlocked.Increment(ref ExecuteCalls); return Task.FromResult(new CatalogPdfExecutionResult(1, 1, 0, 0)); }
        public Task<SingleCatalogPdfResult> UploadSingleAsync(Stream content, string fileName, long length, string? title, CancellationToken cancellationToken = default)
        { Assert.Null(title); Interlocked.Increment(ref SingleCalls); return Task.FromResult(new SingleCatalogPdfResult(Guid.NewGuid(), title!, fileName, "/media/test.pdf", "/media/cover.webp", null)); }
        public Task<SingleCatalogPdfResult> ReplaceAsync(Guid id, Stream content, string fileName, long length, string? title, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<CatalogPdfDocument>> GetPublicDocumentsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CatalogPdfCoverBackfillResult> BackfillCoversAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
