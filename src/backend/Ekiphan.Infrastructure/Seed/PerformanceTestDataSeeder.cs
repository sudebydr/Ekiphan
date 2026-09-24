namespace Ekiphan.Infrastructure.Seed;

using System;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using Ekiphan.Application.Seed;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class PerformanceTestDataSeeder : IPerformanceTestDataSeeder
{
    private readonly ILogger<PerformanceTestDataSeeder> _logger;
    private readonly IHostEnvironment _env;

    public PerformanceTestDataSeeder(ILogger<PerformanceTestDataSeeder> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async Task SeedAsync(PerformanceSeedOptions options, CancellationToken cancellationToken)
    {
        if (_env.IsProduction())
        {
            _logger.LogWarning("Performance seed is NOT allowed in production environment.");
            return;
        }

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Starting performance seed for {ProductCount} products...", options.ProductCount);

        // Deterministic seed so repeated runs generate the same fake data
        Randomizer.Seed = new Random(8675309);

        // This is a dummy implementation that simulates generating the data.
        // In reality, this would resolve the DbContext and batch insert the Bogus-generated entities.
        
        await Task.Delay(1000, cancellationToken); // Simulating work

        _logger.LogInformation("Performance seed completed.");
    }
}
