using Ekiphan.Application.Identity;
using Ekiphan.Application.Quotes;
using Ekiphan.Domain.Quotes;
using Ekiphan.Infrastructure.Quotes;

namespace Ekiphan.UnitTests.Quotes;

public sealed class QuoteManagementServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 28, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ValidTransitionRecordsAdminAndPersists()
    {
        var version = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var quote = CreateQuote(version);
        var repository = new StubRepository(quote);
        var service = CreateService(repository);
        var userId = Guid.NewGuid();

        var result = await service.ChangeStatusAsync(
            new ChangeQuoteStatusCommand(
                quote.Id,
                QuoteStatus.Reviewing,
                version,
                userId,
                "İncelemeye alındı."));

        Assert.Equal(QuoteStatus.Reviewing, result.Status);
        Assert.Equal(1, repository.SaveCount);
        var history = quote.StatusHistory.Last();
        Assert.Equal(userId, history.ChangedByUserId);
        Assert.Equal(Now, history.ChangedAt);
    }

    [Fact]
    public async Task StaleVersionIsRejectedBeforeMutation()
    {
        var quote = CreateQuote(
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var repository = new StubRepository(quote);
        var service = CreateService(repository);

        await Assert.ThrowsAsync<QuoteConcurrencyException>(
            () => service.ChangeStatusAsync(
                new ChangeQuoteStatusCommand(
                    quote.Id,
                    QuoteStatus.Reviewing,
                    new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 },
                    Guid.NewGuid(),
                    null)));

        Assert.Equal(QuoteStatus.New, quote.Status);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task InvalidDomainTransitionIsRejected()
    {
        var version = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var quote = CreateQuote(version);
        var repository = new StubRepository(quote);
        var service = CreateService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ChangeStatusAsync(
                new ChangeQuoteStatusCommand(
                    quote.Id,
                    QuoteStatus.Won,
                    version,
                    Guid.NewGuid(),
                    null)));

        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task AssignmentAndNoteAreAudited()
    {
        var version = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var quote = CreateQuote(version);
        var repository = new StubRepository(quote);
        var service = CreateService(repository);
        var operatorId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();

        await service.AssignAsync(new AssignQuoteCommand(
            quote.Id, assigneeId, version, operatorId));
        await service.AddNoteAsync(new AddQuoteNoteCommand(
            quote.Id, "Müşteri aranacak.", version, operatorId));

        Assert.Equal(assigneeId, quote.AssignedToUserId);
        Assert.Equal(2, quote.InternalNotes.Count);
        Assert.All(quote.InternalNotes,
            item => Assert.Equal(operatorId, item.AuthorUserId));
        Assert.Equal(2, repository.SaveCount);
    }

    private static QuoteManagementService CreateService(StubRepository repository) =>
        new(
            repository,
            new QuoteStatusTransitionService(),
            new NoOpActivityService(),
            new NoOpAuditLogService(),
            new StubTimeProvider());

    private sealed class NoOpActivityService : IQuoteActivityService
    {
        public Task LogAsync(Guid quoteId, QuoteActivityType activityType, Guid? actorUserId, string? previousValue, string? newValue, string description, object? metadata, QuoteRequestContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<QuoteActivityDto>> GetActivitiesAsync(Guid quoteId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<QuoteActivityDto>>([]);
    }

    private sealed class NoOpAuditLogService : IAuditLogService
    {
        public Task WriteAsync(Guid? actor, string action, string category, string entityType, string? entityId, Guid? target, object? oldValues, object? newValues, string? reason, AdminSecurityContext context, bool success, string? failure, CancellationToken ct) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<AuditLogListItemDto>> ListAsync(int page, int pageSize, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<AuditLogListItemDto>>([]);

        public Task<AuditLogDetailDto?> GetAsync(Guid id, bool includeDetails, CancellationToken ct) =>
            Task.FromResult<AuditLogDetailDto?>(null);
    }

    internal static QuoteRequest CreateQuote(byte[] rowVersion)
    {
        var quote = new QuoteRequest(
            Guid.NewGuid(),
            "Q-20260728-ABCDEF12",
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
            DateTimeOffset.UtcNow,
            "kvkk-v1");
        typeof(QuoteRequest)
            .GetProperty(nameof(QuoteRequest.RowVersion))!
            .SetValue(quote, rowVersion);
        return quote;
    }

    internal sealed class StubRepository(QuoteRequest? quote)
        : IQuoteManagementRepository
    {
        public int SaveCount { get; private set; }

        public Task<QuoteRequest?> GetAsync(
            Guid quoteId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                quote?.Id == quoteId ? quote : null);

        public Task<bool> CanAssignUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
