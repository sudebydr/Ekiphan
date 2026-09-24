using Ekiphan.Application.Quotes;
using Ekiphan.Domain.Quotes;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ekiphan.Infrastructure.Quotes.Background;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Worker log statements.")]
public sealed class QuoteEmailQueueWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<QuoteEmailOptions> options,
    ILogger<QuoteEmailQueueWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("QuoteEmailQueueWorker started.");
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEmailsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while processing email queue in worker.");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }

        logger.LogInformation("QuoteEmailQueueWorker stopped.");
    }

    private async Task ProcessPendingEmailsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EkiphanDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var opt = options.Value;

        var now = timeProvider.GetUtcNow();

        var pendingItems = await dbContext.EmailQueueItems
            .Where(x => (x.Status == EmailQueueStatus.Pending || x.Status == EmailQueueStatus.Failed) &&
                        x.NextAttemptAt <= now)
            .OrderBy(x => x.NextAttemptAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (pendingItems.Count == 0) return;

        foreach (var item in pendingItems)
        {
            item.MarkProcessing(now);
            await dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                // Simulated email dispatch (SMTP / MailProvider execution point)
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Sending email [{TemplateCode}] to {Recipient} (Attempt {AttemptCount})",
                        item.TemplateCode, item.Recipient, item.AttemptCount);
                }

                // Mark successful
                item.MarkSent(timeProvider.GetUtcNow());
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed sending email to {Recipient}", item.Recipient);

                var retryDelays = opt.RetryDelaysMinutes.Count > 0 ? opt.RetryDelaysMinutes : [1, 5, 15, 60, 180];
                var delayIndex = Math.Min(item.AttemptCount - 1, retryDelays.Count - 1);
                var delayMinutes = retryDelays[Math.Max(0, delayIndex)];

                item.MarkFailed(timeProvider.GetUtcNow(), "SMTP_ERROR", ex.Message, TimeSpan.FromMinutes(delayMinutes));
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
