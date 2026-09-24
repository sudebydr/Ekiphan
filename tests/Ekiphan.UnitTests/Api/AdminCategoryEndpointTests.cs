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

public sealed class AdminCategoryEndpointTests
{
    private const string SigningKey =
        "test-only-signing-key-at-least-32-bytes";

    [Fact]
    public async Task EndpointsFailClosedWithoutJwtConfiguration()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/admin/catalog/structure");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizedStructureReturnsPrivateResponse()
    {
        var service = new StubService();
        await using var factory = CreateFactory(service, JwtSettings());
        using var client = AuthorizedClient(factory);

        using var response = await client.GetAsync(
            "/api/admin/catalog/structure");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(service.StructureRequested);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task CreateCategoryBindsHierarchyAndTranslations()
    {
        var service = new StubService();
        await using var factory = CreateFactory(service, JwtSettings());
        using var client = AuthorizedClient(factory);
        var sectionId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        using var response = await client.PostAsJsonAsync(
            "/api/admin/catalog/categories",
            new
            {
                productSectionId = sectionId,
                parentId,
                sortOrder = 4,
                isPublished = true,
                translations = new[]
                {
                    new
                    {
                        languageCode = "tr",
                        name = "Kesici Takımlar",
                        slug = "kesici-takimlar",
                        description = "Kategori açıklaması",
                    },
                },
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(service.CreatedCategory);
        Assert.Equal(sectionId, service.CreatedCategory.ProductSectionId);
        Assert.Equal(parentId, service.CreatedCategory.ParentId);
        Assert.Equal(
            "Kesici Takımlar",
            Assert.Single(service.CreatedCategory.Translations).Name);
    }

    [Fact]
    public async Task DeleteCategoryUsesSafeArchiveOperation()
    {
        var service = new StubService();
        await using var factory = CreateFactory(service, JwtSettings());
        using var client = AuthorizedClient(factory);
        var categoryId = Guid.NewGuid();

        using var response = await client.DeleteAsync(
            $"/api/admin/catalog/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(categoryId, service.ArchivedCategoryId);
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
                        "Database=AdminCategoryEndpointTests;" +
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
                                services.RemoveAll<IAdminCategoryService>();
                                services.AddSingleton<IAdminCategoryService>(
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
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private sealed class StubService : IAdminCategoryService
    {
        public bool StructureRequested { get; private set; }
        public SaveAdminCategoryCommand? CreatedCategory { get; private set; }
        public Guid? ArchivedCategoryId { get; private set; }

        public Task<AdminCatalogStructure> GetAsync(
            CancellationToken cancellationToken = default)
        {
            StructureRequested = true;
            return Task.FromResult(new AdminCatalogStructure([], []));
        }

        public Task<AdminSectionDetail> CreateSectionAsync(
            SaveAdminSectionCommand command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Section(Guid.NewGuid()));

        public Task<AdminSectionDetail?> UpdateSectionAsync(
            Guid sectionId,
            SaveAdminSectionCommand command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AdminSectionDetail?>(Section(sectionId));

        public Task<AdminCategoryDetail> CreateCategoryAsync(
            SaveAdminCategoryCommand command,
            CancellationToken cancellationToken = default)
        {
            CreatedCategory = command;
            return Task.FromResult(Category(Guid.NewGuid(), command));
        }

        public Task<AdminCategoryDetail?> UpdateCategoryAsync(
            Guid categoryId,
            SaveAdminCategoryCommand command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AdminCategoryDetail?>(Category(categoryId, command));

        public Task<bool> ArchiveCategoryAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            ArchivedCategoryId = categoryId;
            return Task.FromResult(true);
        }

        private static AdminSectionDetail Section(Guid id) =>
            new(
                id,
                "CUTTING",
                true,
                0,
                [new AdminSectionTranslation("tr", "Kesici Takımlar", "kesici-takimlar")],
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow);

        private static AdminCategoryDetail Category(
            Guid id,
            SaveAdminCategoryCommand command) =>
            new(
                id,
                command.ProductSectionId,
                command.ParentId,
                command.IsPublished,
                command.SortOrder,
                0,
                command.Translations.Select(item => new AdminCategoryTranslation(
                    item.LanguageCode,
                    item.Name,
                    item.Slug,
                    item.Description)).ToArray(),
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow);
    }
}
