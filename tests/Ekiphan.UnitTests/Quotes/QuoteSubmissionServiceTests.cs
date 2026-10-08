using Ekiphan.Application.Quotes;
using Ekiphan.Domain.Quotes;

namespace Ekiphan.UnitTests.Quotes;

public sealed class QuoteSubmissionServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 28, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task SubmissionBuildsSnapshotsFromPublishedCatalogData()
    {
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var repository = new StubRepository(
            new QuoteProductSnapshot(
                productId,
                "Sunucudaki Ürün",
                "BASE-1",
                "Ekiphan",
                "products/base-1.webp",
                new Dictionary<Guid, QuoteVariantSnapshot>
                {
                    [variantId] = new(
                        variantId,
                        "VARIANT-1",
                        "Renk: Şeffaf"),
                }));
        var service = CreateService(repository);

        var result = await service.SubmitAsync(
            ValidCommand(
                new SubmitQuoteItem(productId, variantId, 12, "Proje notu")),
            "kvkk-2026-v1",
            "commercial-2026-v1");

        Assert.Equal("Q-TEST-0001", result.RequestNumber);
        Assert.Equal(Now, result.ReceivedAt);
        var request = Assert.IsType<QuoteRequest>(repository.Added);
        var item = Assert.Single(request.Items);
        Assert.Equal("Sunucudaki Ürün", item.ProductName);
        Assert.Equal("VARIANT-1", item.SKU);
        Assert.Equal("Renk: Şeffaf", item.VariantSnapshot);
        Assert.Equal("products/base-1.webp", item.ImageStorageKey);
        Assert.Equal("kvkk-2026-v1", request.KvkkConsentVersion);
        Assert.Null(request.CommercialCommunicationConsentAt);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task CommercialConsentSnapshotsTimestampAndVersionTogether()
    {
        var product = Product();
        var repository = new StubRepository(product);
        var service = CreateService(repository);
        var command = ValidCommand(
            new SubmitQuoteItem(product.ProductId, null, 1, null)) with
        {
            CommercialCommunicationConsent = true,
        };

        await service.SubmitAsync(
            command,
            "kvkk-v1",
            "commercial-v2");

        Assert.Equal(Now, repository.Added?.CommercialCommunicationConsentAt);
        Assert.Equal(
            "commercial-v2",
            repository.Added?.CommercialCommunicationConsentVersion);
    }

    [Fact]
    public async Task UnavailableProductIsRejectedWithoutPersistence()
    {
        var repository = new StubRepository();
        var service = CreateService(repository);

        await Assert.ThrowsAsync<QuoteSubmissionException>(
            () => service.SubmitAsync(
                ValidCommand(
                    new SubmitQuoteItem(Guid.NewGuid(), null, 1, null)),
                "kvkk-v1",
                "commercial-v1"));

        Assert.Null(repository.Added);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task HoneypotValueIsRejectedWithoutPersistence()
    {
        var product = Product();
        var repository = new StubRepository(product);
        var service = CreateService(repository);
        var command = ValidCommand(
            new SubmitQuoteItem(product.ProductId, null, 1, null)) with
        {
            Website = "https://spam.example",
        };

        await Assert.ThrowsAsync<QuoteSubmissionException>(
            () => service.SubmitAsync(
                command,
                "kvkk-v1",
                "commercial-v1"));

        Assert.Null(repository.Added);
    }

    [Fact]
    public void RequestNumberUsesDateAndCryptographicRandomSuffix()
    {
        var generator = new QuoteRequestNumberGenerator();

        var first = generator.Generate(Now);
        var second = generator.Generate(Now);

        Assert.Matches("^Q-20260728-[0-9A-F]{16}$", first);
        Assert.NotEqual(first, second);
        Assert.True(first.Length <= 30);
    }

    private static QuoteSubmissionService CreateService(
        StubRepository repository) =>
        new(
            repository,
            new StubNumberGenerator(),
            new SuccessfulBotVerificationService(),
            new AllowSpamDetectionService(),
            new NoOpNotificationService(),
            new NoOpActivityService(),
            new StubTimeProvider());

    private static SubmitQuoteCommand ValidCommand(
        params SubmitQuoteItem[] items) =>
        new(
            "Test Kullanıcı",
            "Test Şirketi",
            "+90 555 000 00 00",
            "test@example.com",
            "Türkiye",
            "İstanbul",
            "Otel",
            "Yeni Proje",
            "Bilgi rica ederiz.",
            "tr",
            true,
            false,
            null,
            items);

    private static QuoteProductSnapshot Product()
    {
        var id = Guid.NewGuid();
        return new QuoteProductSnapshot(
            id,
            "Ürün",
            "SKU-1",
            null,
            null,
            new Dictionary<Guid, QuoteVariantSnapshot>());
    }

    private sealed class StubRepository(
        params QuoteProductSnapshot[] products)
        : IQuoteSubmissionRepository
    {
        private readonly IReadOnlyDictionary<Guid, QuoteProductSnapshot>
            _products = products.ToDictionary(product => product.ProductId);

        public QuoteRequest? Added { get; private set; }

        public int SaveCount { get; private set; }

        public Task<IReadOnlyDictionary<Guid, QuoteProductSnapshot>>
            GetProductSnapshotsAsync(
                IReadOnlyCollection<Guid> productIds,
                string languageCode,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(
                (IReadOnlyDictionary<Guid, QuoteProductSnapshot>)_products
                    .Where(item => productIds.Contains(item.Key))
                    .ToDictionary());

        public void Add(QuoteRequest quoteRequest)
        {
            Added = quoteRequest;
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    internal sealed class SuccessfulBotVerificationService : IBotVerificationService
    {
        public Task<BotVerificationResult> VerifyAsync(string? token, string? ipAddress, CancellationToken cancellationToken = default) =>
            Task.FromResult(new BotVerificationResult(true, 1, null, null));
    }

    internal sealed class AllowSpamDetectionService : IQuoteSpamDetectionService
    {
        public Task<QuoteSpamCheckResult> CheckAsync(SubmitQuoteCommand command, QuoteRequestContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(new QuoteSpamCheckResult(false, 0, [], SpamRecommendedAction.Allow));
    }

    internal sealed class NoOpNotificationService : IQuoteNotificationService
    {
        public Task QueueNewQuoteNotificationsAsync(Guid quoteId, QuoteRequestContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    internal sealed class NoOpActivityService : IQuoteActivityService
    {
        public Task LogAsync(Guid quoteId, QuoteActivityType activityType, Guid? actorUserId, string? previousValue, string? newValue, string description, object? metadata, QuoteRequestContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<QuoteActivityDto>> GetActivitiesAsync(Guid quoteId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<QuoteActivityDto>>([]);
    }

    private sealed class StubNumberGenerator : IQuoteRequestNumberGenerator
    {
        public string Generate(DateTimeOffset timestamp) => "Q-TEST-0001";
    }

    private sealed class StubTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
