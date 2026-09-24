namespace Ekiphan.Infrastructure.Warmup;

using System;
using System.Threading;
using System.Threading.Tasks;
using Ekiphan.Application.Warmup;
using Ekiphan.Application.Warmup.Models;
using Microsoft.Extensions.Logging;

public class CacheWarmupService : ICacheWarmupService
{
    private readonly ILogger<CacheWarmupService> _logger;
    
    public CacheWarmupService(ILogger<CacheWarmupService> logger)
    {
        _logger = logger;
    }

    public async Task WarmupAsync(CacheWarmupRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting cache warmup...");
        
        if (request.WarmupCategoryTree)
        {
            _logger.LogInformation("Warming up category tree...");
            await Task.Delay(100, cancellationToken); // Simulate DB fetch & cache set
        }

        if (request.WarmupBrandList)
        {
            _logger.LogInformation("Warming up brand list...");
            await Task.Delay(100, cancellationToken);
        }

        // Add other warmup tasks here
        
        _logger.LogInformation("Cache warmup completed.");
    }
}
