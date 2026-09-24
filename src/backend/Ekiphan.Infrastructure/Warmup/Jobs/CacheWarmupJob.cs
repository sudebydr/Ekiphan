namespace Ekiphan.Infrastructure.Warmup.Jobs;

using System.Threading;
using System.Threading.Tasks;
using Ekiphan.Application.Caching;
using Ekiphan.Application.Warmup;
using Ekiphan.Application.Warmup.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

public class CacheWarmupJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CacheWarmupJob> _logger;

    public CacheWarmupJob(IServiceScopeFactory scopeFactory, ILogger<CacheWarmupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const string lockKey = "lock:cache-warmup:startup";
        
        // Wait a bit before starting to not block startup completely
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        
        using var scope = _scopeFactory.CreateScope();
        var lockService = scope.ServiceProvider.GetRequiredService<IDistributedLockService>();
        var warmupService = scope.ServiceProvider.GetRequiredService<ICacheWarmupService>();
        
        if (await lockService.TryAcquireLockAsync(lockKey, TimeSpan.FromMinutes(5), stoppingToken))
        {
            try
            {
                var request = new CacheWarmupRequest(); // Defaults to true for all
                await warmupService.WarmupAsync(request, stoppingToken);
            }
            finally
            {
                await lockService.ReleaseLockAsync(lockKey, stoppingToken);
            }
        }
        else
        {
            _logger.LogInformation("Cache warmup lock could not be acquired (likely already running on another instance).");
        }
    }
}
