using System.Text.Json;
using Ekiphan.Application.Quotes;
using Ekiphan.Domain.Quotes;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Quotes;

public sealed class QuoteSpamDetectionService(
    EkiphanDbContext dbContext,
    TimeProvider timeProvider) : IQuoteSpamDetectionService
{
    public async Task<QuoteSpamCheckResult> CheckAsync(
        SubmitQuoteCommand command,
        QuoteRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var reasons = new List<string>();
        int score = 0;

        // 1. Honeypot field check
        if (!string.IsNullOrWhiteSpace(command.Website))
        {
            score += 100;
            reasons.Add("Honeypot field filled.");
        }

        // 2. Suspicious script or link content check
        if (!string.IsNullOrWhiteSpace(command.Message))
        {
            var msg = command.Message.ToLowerInvariant();
            if (msg.Contains("<script") || msg.Contains("javascript:") || msg.Contains("eval("))
            {
                score += 80;
                reasons.Add("Potential XSS script tag detected in message.");
            }
            if (msg.Split("http://", StringSplitOptions.None).Length - 1 + (msg.Split("https://", StringSplitOptions.None).Length - 1) > 3)
            {
                score += 40;
                reasons.Add("Excessive external links in message.");
            }
        }

        // 3. Short time window duplicate check from same IP / Email / Phone
        var now = timeProvider.GetUtcNow();
        var window = now.AddMinutes(-15);

        if (!string.IsNullOrWhiteSpace(context.IpAddress))
        {
            var ipAttempts = await dbContext.AdminLoginAttempts.AsNoTracking()
                .CountAsync(x => x.IpAddress == context.IpAddress && x.AttemptedAt >= window, cancellationToken);
            if (ipAttempts > 10)
            {
                score += 30;
                reasons.Add("High request volume from IP in recent window.");
            }
        }

        var normalizedEmail = command.Email.Trim().ToUpperInvariant();
        var recentEmailQuotes = await dbContext.QuoteRequests.AsNoTracking()
            .CountAsync(x => x.Email == normalizedEmail && x.CreatedAt >= window, cancellationToken);

        if (recentEmailQuotes > 3)
        {
            score += 50;
            reasons.Add("Multiple quote requests submitted from same email in short window.");
        }

        // Determine recommended action
        SpamRecommendedAction action = score >= 80 ? SpamRecommendedAction.Reject
            : score >= 50 ? SpamRecommendedAction.RequireReview
            : score >= 30 ? SpamRecommendedAction.AllowAndFlag
            : SpamRecommendedAction.Allow;

        return new QuoteSpamCheckResult(
            score >= 80,
            score,
            reasons,
            action);
    }
}
