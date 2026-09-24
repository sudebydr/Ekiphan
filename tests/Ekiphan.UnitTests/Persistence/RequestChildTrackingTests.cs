using Ekiphan.Domain.Content;
using Ekiphan.Domain.Quotes;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.UnitTests.Persistence;

public sealed class RequestChildTrackingTests
{
    [Fact]
    public void QuoteMutationsTrackClientGeneratedChildrenAsAdded()
    {
        using var context = CreateContext();
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var quote = new QuoteRequest(
            Guid.NewGuid(),
            "Q-TRACKING-1",
            "Test User",
            "Test Company",
            "+90 555 000 0000",
            "tracking@ekiphan.invalid",
            "Türkiye",
            null,
            null,
            null,
            null,
            "tr",
            now,
            "test-kvkk-v1");

        context.Attach(quote);
        quote.TransitionTo(QuoteStatus.Reviewing, now.AddMinutes(1), actor);
        var note = quote.AddInternalNote(actor, "Tracking check", now.AddMinutes(1));
        context.ChangeTracker.DetectChanges();

        Assert.Equal(
            EntityState.Added,
            context.Entry(quote.StatusHistory.Last()).State);
        Assert.Equal(EntityState.Added, context.Entry(note).State);
    }

    [Fact]
    public void ContactMutationsTrackClientGeneratedChildrenAsAdded()
    {
        using var context = CreateContext();
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var request = new ContactRequest(
            Guid.NewGuid(),
            "Test User",
            "tracking@ekiphan.invalid",
            null,
            null,
            "Tracking",
            "Tracking verification message.",
            "tr",
            now,
            "test-kvkk-v1");

        context.Attach(request);
        request.TransitionTo(ContactRequestStatus.Read, actor, now.AddMinutes(1));
        var note = request.AddInternalNote(actor, "Tracking check", now.AddMinutes(1));
        context.ChangeTracker.DetectChanges();

        Assert.Equal(
            EntityState.Added,
            context.Entry(request.StatusHistory.Last()).State);
        Assert.Equal(EntityState.Added, context.Entry(note).State);
    }

    private static EkiphanDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<EkiphanDbContext>()
            .UseSqlServer(
                "Server=tracking-only;Database=TrackingOnly;Integrated Security=True;" +
                "TrustServerCertificate=True")
            .Options);
}
