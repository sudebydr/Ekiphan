using Ekiphan.Application.Catalog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Scope = "module", Justification = "Background worker logger.")]

namespace Ekiphan.Infrastructure.Catalog.Background;

public sealed class ProductBulkOperationWorker(
    IServiceProvider serviceProvider,
    IProductBulkOperationQueue queue,
    ILogger<ProductBulkOperationWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ProductBulkOperationWorker started.");
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (queue is InMemoryProductBulkOperationQueue inMemoryQueue && inMemoryQueue.TryDequeue(out var operationId))
                {
                    if (logger.IsEnabled(LogLevel.Information))
                        logger.LogInformation("Processing bulk operation {OperationId}.", operationId);
                    using var scope = serviceProvider.CreateScope();
                    var bulkService = scope.ServiceProvider.GetRequiredService<IProductBulkOperationService>();
                    if (bulkService is ProductBulkOperationService impl)
                    {
                        await impl.ProcessBulkOperationCoreAsync(operationId, stoppingToken);
                    }
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing bulk operation queue item.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
