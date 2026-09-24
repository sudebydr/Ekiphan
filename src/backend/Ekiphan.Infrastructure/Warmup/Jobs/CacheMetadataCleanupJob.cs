namespace Ekiphan.Infrastructure.Warmup.Jobs;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class CacheMetadataCleanupJob : BackgroundService
{
    private readonly ILogger<CacheMetadataCleanupJob> _logger;

    public CacheMetadataCleanupJob(ILogger<CacheMetadataCleanupJob> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run periodically every few hours
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(4), stoppingToken);
            
            _logger.LogInformation("Starting cache metadata cleanup...");
            // Simulate Redis SCAN and orphaned tag removal
            await Task.Delay(500, stoppingToken);
            _logger.LogInformation("Cache metadata cleanup completed.");
        }
    }
}
