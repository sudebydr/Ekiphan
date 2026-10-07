using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Ekiphan.Application.DataImport;
using Ekiphan.Domain.Catalog;
using Ekiphan.Application.Identity;
using Ekiphan.Domain.DataImport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ekiphan.UnitTests.Api;

public sealed class ImportUploadEndpointTests
{
    private const string SigningKey =
        "test-only-signing-key-at-least-32-bytes";

    [Fact]
    public async Task UploadReturnsServiceUnavailableWhenAuthenticationIsNotConfigured()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            "/api/admin/imports",
            new ByteArrayContent([]));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task UploadRequiresBearerTokenWhenJwtIsConfigured()
    {
        await using var factory = CreateFactory(
            new Dictionary<string, string?>
            {
                ["Authentication:Jwt:Issuer"] = "https://issuer.example",
                ["Authentication:Jwt:Audience"] = "ekiphan-admin",
                ["Authentication:Jwt:SigningKey"] = SigningKey
            });
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            "/api/admin/imports",
            new ByteArrayContent([]));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task HealthRemainsAvailableWithoutAuthentication()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizedUploadStagesValidCsv()
    {
        var repository = new StubRepository();
        await using var factory = CreateFactory(
            JwtSettings(),
            services =>
            {
                services.RemoveAll<IImportJobRepository>();
                services.RemoveAll<IImportReferenceResolver>();
                services.AddSingleton<IImportJobRepository>(repository);
                services.AddSingleton<IImportReferenceResolver>(new StubReferenceResolver());
            });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateAccessToken());
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(
            Encoding.UTF8.GetBytes("Ürün Kodu;Ürün Adı\r\nABC-1;Tabak"));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(file, "file", "products.csv");
        form.Add(new StringContent("true"), "isDryRun");

        using var response = await client.PostAsync("/api/admin/imports", form);
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, body);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(
            (int)ImportJobStatus.Completed,
            json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(1, repository.SaveCount);
        Assert.NotNull(repository.AddedJob);
    }

    [Fact]
    public async Task AuthorizedDuplicateUploadReturnsControlledTurkishConflict()
    {
        var repository = new StubRepository { SourceExists = true };
        await using var factory = CreateFactory(
            JwtSettings(),
            services =>
            {
                services.RemoveAll<IImportJobRepository>();
                services.AddSingleton<IImportJobRepository>(repository);
            });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateAccessToken());
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes("SKU,Urun Adi\r\nABC-1,Tabak"));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(file, "file", "products.csv");
        form.Add(new StringContent("false"), "isDryRun");

        using var response = await client.PostAsync("/api/admin/imports", form);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("Aynı import kaynağı", body);
        Assert.DoesNotContain("An error occurred while processing your request.", body);
    }

    [Fact]
    public async Task AuthorizedJobListBindsPaginationAndStatus()
    {
        var queryService = new StubQueryService();
        await using var factory = CreateFactory(
            JwtSettings(),
            services =>
            {
                services.RemoveAll<IImportQueryService>();
                services.AddSingleton<IImportQueryService>(queryService);
            });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateAccessToken());

        using var response = await client.GetAsync(
            "/api/admin/imports?page=2&pageSize=10&status=Completed");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, queryService.RequestedPage);
        Assert.Equal(10, queryService.RequestedPageSize);
        Assert.Equal(ImportJobStatus.Completed, queryService.RequestedStatus);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(2, json.RootElement.GetProperty("page").GetInt32());
    }

    [Fact]
    public async Task AuthorizedMissingJobReturnsNotFound()
    {
        await using var factory = CreateFactory(
            JwtSettings(),
            services =>
            {
                services.RemoveAll<IImportQueryService>();
                services.AddSingleton<IImportQueryService>(
                    new StubQueryService());
            });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateAccessToken());

        using var response = await client.GetAsync(
            $"/api/admin/imports/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizedIssueDownloadStreamsCsvAttachment()
    {
        var jobId = Guid.NewGuid();
        var queryService = new StubQueryService
        {
            Job = CreateJobDetail(jobId)
        };
        var reportWriter = new StubReportWriter();
        await using var factory = CreateFactory(
            JwtSettings(),
            services =>
            {
                services.RemoveAll<IImportQueryService>();
                services.RemoveAll<IImportIssueReportWriter>();
                services.AddSingleton<IImportQueryService>(queryService);
                services.AddSingleton<IImportIssueReportWriter>(reportWriter);
            });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateAccessToken());

        using var response = await client.GetAsync(
            $"/api/admin/imports/{jobId}/issues.csv?severity=Error");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(
            $"import-{jobId:N}-issues.csv",
            response.Content.Headers.ContentDisposition?.FileName);
        Assert.Equal(ImportIssueSeverity.Error, reportWriter.Severity);
        Assert.Equal("report", body);
    }

    [Fact]
    public async Task PublishRejectsTokenWithoutPublishPermission()
    {
        await using var factory = CreateFactory(JwtSettings());
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateAccessToken());

        using var response = await client.PostAsync(
            $"/api/admin/imports/{Guid.NewGuid()}/publish",
            content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizedPublishCompletesReadyJob()
    {
        var job = CreateReadyImportJob();
        var repository = new StubPublishingRepository(job);
        await using var factory = CreateFactory(
            JwtSettings(),
            services =>
            {
                services.RemoveAll<IImportPublishingRepository>();
                services.AddSingleton<IImportPublishingRepository>(repository);
            });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateAccessToken("imports.publish"));

        using var response = await client.PostAsync(
            $"/api/admin/imports/{job.Id}/publish",
            content: null);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(1, json.RootElement.GetProperty("publishedRowCount").GetInt32());
        Assert.Single(repository.Products);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IReadOnlyDictionary<string, string?>? settings = null,
        Action<IServiceCollection>? configureServices = null)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder =>
                {
                    builder.UseSetting(
                        "ConnectionStrings:EkiphanDatabase",
                        "Server=(localdb)\\mssqllocaldb;Database=EndpointTests;" +
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
    }

    private static Dictionary<string, string?> JwtSettings() =>
        new()
        {
            ["Authentication:Jwt:Issuer"] = "https://issuer.example",
            ["Authentication:Jwt:Audience"] = "ekiphan-admin",
            ["Authentication:Jwt:SigningKey"] = SigningKey
        };

    private static string CreateAccessToken(
        string permission = "imports.manage")
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
                ["permission"] = permission
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static ImportJob CreateReadyImportJob()
    {
        var job = new ImportJob(
            Guid.NewGuid(),
            ImportSourceType.Csv,
            "products.csv",
            new string('B', 64),
            isDryRun: false);
        job.StartValidation(DateTimeOffset.UtcNow);
        var row = job.AddRow(
            Guid.NewGuid(),
            "Products",
            2,
            """{"SKU":"NEW-1","Name":"Yeni Ürün"}""",
            "NEW-1");
        row.MarkValid("""{"sku":"NEW-1","name":"Yeni Ürün"}""");
        job.CompleteValidation(DateTimeOffset.UtcNow);
        return job;
    }

    private static ImportJobDetail CreateJobDetail(Guid id) =>
        new(
            id,
            "products.csv",
            ImportSourceType.Csv,
            ImportJobStatus.ValidationFailed,
            true,
            new string('A', 64),
            null,
            1,
            0,
            1,
            0,
            0,
            null,
            null,
            null,
            null,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

    private sealed class StubRepository : IImportJobRepository
    {
        public bool SourceExists { get; init; }

        public int SaveCount { get; private set; }

        public ImportJob? AddedJob { get; private set; }

        public Task<bool> SourceExistsAsync(
            string sourceSha256Checksum,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(SourceExists);

        public void Add(ImportJob job)
        {
            AddedJob = job;
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubReferenceResolver : IImportReferenceResolver
    {
        public Task<ImportReferenceResolution> ResolveAsync(
            IReadOnlyCollection<string> brandNames,
            IReadOnlyCollection<IReadOnlyList<string>> categoryPaths,
            IReadOnlyCollection<string> materialNames,
            IReadOnlyCollection<string> tagNames,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                new ImportReferenceResolution(
                    new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase),
                    new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase),
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    null,
                    new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase),
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase),
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase)));
    }

    private sealed class StubQueryService : IImportQueryService
    {
        public ImportJobDetail? Job { get; init; }

        public int RequestedPage { get; private set; }

        public int RequestedPageSize { get; private set; }

        public ImportJobStatus? RequestedStatus { get; private set; }

        public Task<PagedResult<ImportJobSummary>> GetJobsAsync(
            int page,
            int pageSize,
            ImportJobStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            RequestedPage = page;
            RequestedPageSize = pageSize;
            RequestedStatus = status;
            return Task.FromResult(
                new PagedResult<ImportJobSummary>([], page, pageSize, 0));
        }

        public Task<ImportJobDetail?> GetJobAsync(
            Guid jobId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Job);

        public Task<PagedResult<ImportIssueItem>?> GetIssuesAsync(
            Guid jobId,
            int page,
            int pageSize,
            ImportIssueSeverity? severity = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<PagedResult<ImportIssueItem>?>(
                new PagedResult<ImportIssueItem>([], page, pageSize, 0));
    }

    private sealed class StubReportWriter : IImportIssueReportWriter
    {
        public ImportIssueSeverity? Severity { get; private set; }

        public async Task WriteCsvAsync(
            Guid jobId,
            Stream destination,
            ImportIssueSeverity? severity = null,
            CancellationToken cancellationToken = default)
        {
            Severity = severity;
            await destination.WriteAsync(
                Encoding.UTF8.GetBytes("report"),
                cancellationToken);
        }
    }

    private sealed class StubPublishingRepository(ImportJob job)
        : IImportPublishingRepository
    {
        public List<Ekiphan.Domain.Catalog.Product> Products { get; } = [];

        public Task<ImportJob?> GetJobAsync(
            Guid jobId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ImportJob?>(job);

        public Task<HashSet<string>> GetExistingSkusAsync(
            IReadOnlyCollection<string> skus,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        public Task<IReadOnlyDictionary<string, Ekiphan.Domain.Catalog.Product>> GetProductsBySkusAsync(
            IReadOnlyCollection<string> skus,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, Ekiphan.Domain.Catalog.Product>>(
                new Dictionary<string, Ekiphan.Domain.Catalog.Product>(StringComparer.OrdinalIgnoreCase));

        public void AddProduct(Ekiphan.Domain.Catalog.Product product)
        {
            Products.Add(product);
        }

        public void AddAttributeValue(
            Ekiphan.Domain.Catalog.ProductAttributeValue value)
        {
        }

        public void AddIssue(Ekiphan.Domain.DataImport.ImportIssue issue)
        {
        }

        public Task PrepareProductDataAsync(IReadOnlyCollection<Guid> productIds,
            IReadOnlyCollection<string> attributeCodes, IReadOnlyCollection<Guid> materialAttributeIds,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ReplaceImportedAttributesAsync(Guid productId, IReadOnlyDictionary<string, string[]> values,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ReplaceMaterialAsync(Guid productId, Guid? attributeId, Guid? optionId, string? rawValue,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpsertVariantAsync(Product product, string variantKey, int sortOrder,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<string>> ApplyRelationsAsync(Guid sourceProductId,
            IReadOnlyCollection<string> similarSkus, IReadOnlyCollection<string> complementarySkus,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> ApplyRelationsBatchAsync(
            IReadOnlyCollection<ImportRelationRequest> requests,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<string>>>(
                requests.ToDictionary(request => request.RowId, _ => (IReadOnlyList<string>)[]));

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default) =>
            operation(cancellationToken);
    }
}
