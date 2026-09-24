using Ekiphan.Application.Identity;
using Ekiphan.Application.Quotes;
using Ekiphan.Domain.Quotes;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ekiphan.Infrastructure.Quotes;

public sealed class QuoteRetentionService(
    EkiphanDbContext dbContext,
    IQuoteActivityService activityService,
    IOptions<QuoteRetentionOptions> options,
    TimeProvider timeProvider) : IQuoteRetentionService
{
    public async Task ProcessRetentionPolicyAsync(CancellationToken cancellationToken = default)
    {
        var opt = options.Value;
        if (!opt.AnonymizeAfterRetention) return;

        var threshold = timeProvider.GetUtcNow().AddDays(-opt.RetentionDays);

        var expiredQuotes = await dbContext.QuoteRequests
            .Where(x => !x.IsAnonymized && x.CreatedAt <= threshold)
            .Take(100)
            .ToListAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();
        var context = new QuoteRequestContext("SYSTEM", "RetentionJob", Guid.NewGuid().ToString("N"));

        foreach (var quote in expiredQuotes)
        {
            quote.Anonymize(now);

            await activityService.LogAsync(
                quote.Id,
                QuoteActivityType.Anonymized,
                null,
                null,
                null,
                $"Quote PII anonymized after retention period ({opt.RetentionDays} days).",
                new { AnonymizedAt = now, RetentionDays = opt.RetentionDays },
                context,
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class QuoteDeletionService(
    EkiphanDbContext dbContext,
    IQuoteActivityService activityService,
    IQuoteStatusTransitionService transitionService,
    IReAuthenticationService reAuthenticationService,
    TimeProvider timeProvider) : IQuoteDeletionService
{
    public async Task ArchiveAsync(
        ArchiveQuoteCommand command,
        QuoteRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var quote = await dbContext.QuoteRequests
            .SingleOrDefaultAsync(x => x.Id == command.QuoteId, cancellationToken);

        if (quote is null)
        {
            throw new KeyNotFoundException("Quote not found.");
        }

        var now = timeProvider.GetUtcNow();
        var previousStatus = quote.Status;

        if (!transitionService.CanTransition(previousStatus, QuoteStatus.Archived))
        {
            throw new InvalidOperationException($"Transition from '{previousStatus}' to 'Archived' is invalid.");
        }

        quote.Archive(command.ArchivedByUserId, command.Reason, now);

        await activityService.LogAsync(
            quote.Id,
            QuoteActivityType.Archived,
            command.ArchivedByUserId,
            previousStatus.ToString(),
            QuoteStatus.Archived.ToString(),
            $"Quote archived by administrator. Reason: {command.Reason}",
            new { Reason = command.Reason },
            context,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HardDeleteAsync(
        PermanentDeleteQuoteCommand command,
        QuoteRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var quote = await dbContext.QuoteRequests
            .SingleOrDefaultAsync(x => x.Id == command.QuoteId, cancellationToken);

        if (quote is null)
        {
            throw new KeyNotFoundException("Quote not found.");
        }

        // Validate step-up re-authentication token
        var validToken = await reAuthenticationService.ConsumeAsync(
            command.ReAuthToken,
            command.DeletedByUserId,
            "quotes.delete",
            cancellationToken);

        if (!validToken)
        {
            throw new UnauthorizedAccessException("Re-authentication token is invalid or expired for quote deletion.");
        }

        await activityService.LogAsync(
            quote.Id,
            QuoteActivityType.Deleted,
            command.DeletedByUserId,
            quote.Status.ToString(),
            "DELETED",
            $"Permanent deletion executed by administrator. Reason: {command.Reason}",
            new { Reason = command.Reason },
            context,
            cancellationToken);

        dbContext.QuoteRequests.Remove(quote);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
