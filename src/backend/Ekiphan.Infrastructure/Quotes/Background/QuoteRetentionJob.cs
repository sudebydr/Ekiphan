using Ekiphan.Application.Quotes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ekiphan.Infrastructure.Quotes.Background;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Job log statements.")]
public sealed class QuoteRetentionJob(
    IServiceScopeFactory scopeFactory,
    ILogger<QuoteRetentionJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("QuoteRetentionJob started.");
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var retentionService = scope.ServiceProvider.GetRequiredService<IQuoteRetentionService>();
                await retentionService.ProcessRetentionPolicyAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error running quote retention policy execution.");
            }

            // Run once per 24 hours
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }

        logger.LogInformation("QuoteRetentionJob stopped.");
    }
}
