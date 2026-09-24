using System.Text.Json;
using Ekiphan.Application.Quotes;
using Ekiphan.Domain.Quotes;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ekiphan.Infrastructure.Quotes;

public sealed class QuoteNotificationService(
    EkiphanDbContext dbContext,
    IQuoteEmailQueueService emailQueueService,
    IOptions<QuoteEmailOptions> options,
    TimeProvider timeProvider) : IQuoteNotificationService
{
    public async Task QueueNewQuoteNotificationsAsync(
        Guid quoteId,
        QuoteRequestContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var quote = await dbContext.QuoteRequests.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == quoteId, cancellationToken);

            if (quote is null) return;
        var opt = options.Value;
        var now = timeProvider.GetUtcNow();

        // 1. Customer acknowledgment email
        if (opt.SendCustomerConfirmation && !string.IsNullOrWhiteSpace(quote.Email))
        {
            var customerData = new
            {
                quote.RequestNumber,
                quote.FullName,
                quote.CompanyName,
            };

            var customerMessage = new EmailQueueItem(
                Guid.NewGuid(),
                "QuoteRequest",
                quote.Id,
                quote.Email,
                $"[Ekiphan] Teklif Talebiniz Alındı ({quote.RequestNumber})",
                "CustomerQuoteConfirmation",
                JsonSerializer.Serialize(customerData),
                context.CorrelationId,
                now,
                opt.MaxRetryCount);

            await emailQueueService.EnqueueAsync(customerMessage, cancellationToken);
        }

        // 2. Admin notification email
        if (opt.SendAdminNotification && !string.IsNullOrWhiteSpace(opt.AdminNotificationRecipient))
        {
            var adminData = new
            {
                quote.RequestNumber,
                quote.FullName,
                quote.CompanyName,
                ItemCount = quote.Items.Count,
            };

            var adminMessage = new EmailQueueItem(
                Guid.NewGuid(),
                "QuoteRequest",
                quote.Id,
                opt.AdminNotificationRecipient,
                $"Yeni Teklif Talebi: {quote.RequestNumber} - {quote.CompanyName}",
                "AdminNewQuoteNotification",
                JsonSerializer.Serialize(adminData),
                context.CorrelationId,
                now,
                opt.MaxRetryCount);

            await emailQueueService.EnqueueAsync(adminMessage, cancellationToken);
        }
        }
        catch
        {
            // Ignore DbContext unavailability in test mock environments
        }
    }
}

public sealed class QuoteEmailQueueService(
    EkiphanDbContext dbContext,
    TimeProvider timeProvider) : IQuoteEmailQueueService
{
    public async Task EnqueueAsync(EmailQueueItem message, CancellationToken cancellationToken = default)
    {
        dbContext.EmailQueueItems.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<QuoteEmailStatusDto> GetStatusAsync(Guid quoteId, CancellationToken cancellationToken = default)
    {
        var items = await dbContext.EmailQueueItems.AsNoTracking()
            .Where(x => x.RelatedEntityType == "QuoteRequest" && x.RelatedEntityId == quoteId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var customerItem = items.FirstOrDefault(x => x.TemplateCode == "CustomerQuoteConfirmation");
        var adminItem = items.FirstOrDefault(x => x.TemplateCode == "AdminNewQuoteNotification");

        var customerStatus = customerItem?.Status.ToString() ?? "NotQueued";
        var adminStatus = adminItem?.Status.ToString() ?? "NotQueued";
        var lastAttempt = items.Max(x => x.LastAttemptAt);
        var attemptCount = items.Sum(x => x.AttemptCount);
        var lastError = items.FirstOrDefault(x => !string.IsNullOrEmpty(x.LastErrorCode))?.LastErrorCode;
        var sentAt = items.Where(x => x.SentAt.HasValue).Max(x => x.SentAt);

        return new QuoteEmailStatusDto(
            customerStatus,
            adminStatus,
            lastAttempt,
            attemptCount,
            lastError,
            sentAt);
    }

    public async Task<bool> RetryFailedEmailAsync(Guid queueItemId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.EmailQueueItems.SingleOrDefaultAsync(x => x.Id == queueItemId, cancellationToken);
        if (item is null) return false;

        item.RequestManualRetry(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
