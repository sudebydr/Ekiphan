using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ekiphan.Application.Quotes;
using Ekiphan.Domain.Quotes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ekiphan.UnitTests.Api;

public sealed class QuoteSubmissionEndpointTests
{
    [Fact]
    public async Task SubmissionFailsClosedWhenConsentVersionsAreMissing()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/quotes",
            Request(Guid.NewGuid()));

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
    }

    [Fact]
    public async Task ValidPublicSubmissionReturnsOpaqueRequestNumber()
    {
        var productId = Guid.NewGuid();
        var repository = new StubRepository(
            new QuoteProductSnapshot(
                productId,
                "Katalog Ürünü",
                "SKU-1",
                "Ekiphan",
                null,
                new Dictionary<Guid, QuoteVariantSnapshot>()));
        await using var factory = CreateFactory(repository);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/quotes",
            Request(productId));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(
            "Q-ENDPOINT-TEST",
            json.RootElement.GetProperty("requestNumber").GetString());
        Assert.NotNull(repository.Added);
        Assert.Equal("kvkk-approved-v1", repository.Added.KvkkConsentVersion);
    }

    [Fact]
    public async Task MissingKvkkConsentReturnsProblemDetails()
    {
        var productId = Guid.NewGuid();
        var repository = new StubRepository(
            new QuoteProductSnapshot(
                productId,
                "Katalog Ürünü",
                "SKU-1",
                null,
                null,
                new Dictionary<Guid, QuoteVariantSnapshot>()));
        await using var factory = CreateFactory(repository);
        using var client = factory.CreateClient();
        var request = Request(productId) with
        {
            KvkkConsent = false,
        };

        using var response = await client.PostAsJsonAsync(
            "/api/quotes",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        Assert.Null(repository.Added);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        StubRepository? repository = null) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(
                builder =>
                {
                    builder.UseSetting(
                        "ConnectionStrings:EkiphanDatabase",
                        "Server=(localdb)\\mssqllocaldb;" +
                        "Database=QuoteEndpointTests;" +
                        "Trusted_Connection=True");
                    if (repository is null)
                    {
                        return;
                    }

                    builder.UseSetting(
                        "QuoteConsent:KvkkVersion",
                        "kvkk-approved-v1");
                    builder.UseSetting(
                        "QuoteConsent:CommercialCommunicationVersion",
                        "commercial-approved-v1");
                    builder.ConfigureTestServices(
                        services =>
                        {
                            services.RemoveAll<IQuoteSubmissionRepository>();
                            services.RemoveAll<IQuoteRequestNumberGenerator>();
                            services.AddSingleton<IQuoteSubmissionRepository>(
                                repository);
                            services.AddSingleton<IQuoteRequestNumberGenerator>(
                                new StubNumberGenerator());
                        });
                });

    private static SubmitQuoteCommand Request(Guid productId) =>
        new(
            "Test Kullanıcı",
            "Test Şirketi",
            "+90 555 000 00 00",
            "test@example.com",
            "Türkiye",
            "İstanbul",
            null,
            null,
            null,
            "tr",
            true,
            false,
            null,
            [new SubmitQuoteItem(productId, null, 2, null)]);

    private sealed class StubRepository(QuoteProductSnapshot product)
        : IQuoteSubmissionRepository
    {
        public QuoteRequest? Added { get; private set; }

        public Task<IReadOnlyDictionary<Guid, QuoteProductSnapshot>>
            GetProductSnapshotsAsync(
                IReadOnlyCollection<Guid> productIds,
                string languageCode,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(
                (IReadOnlyDictionary<Guid, QuoteProductSnapshot>)
                new Dictionary<Guid, QuoteProductSnapshot>
                {
                    [product.ProductId] = product,
                });

        public void Add(QuoteRequest quoteRequest)
        {
            Added = quoteRequest;
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubNumberGenerator : IQuoteRequestNumberGenerator
    {
        public string Generate(DateTimeOffset timestamp) =>
            "Q-ENDPOINT-TEST";
    }
}
