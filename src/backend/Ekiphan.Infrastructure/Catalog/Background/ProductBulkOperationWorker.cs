using Ekiphan.Application.Catalog;
using Ekiphan.Domain.Catalog;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

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
        await RecoverPendingOperationsAsync(stoppingToken);

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

    private async Task RecoverPendingOperationsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EkiphanDbContext>();
        var operationIds = await dbContext.ProductBulkOperations
            .AsNoTracking()
            .Where(operation =>
                operation.Status == ProductBulkOperationStatus.Pending ||
                operation.Status == ProductBulkOperationStatus.Processing)
            .OrderBy(operation => operation.RequestedAt)
            .Select(operation => operation.Id)
            .ToListAsync(cancellationToken);

        foreach (var operationId in operationIds)
        {
            await queue.EnqueueAsync(operationId, cancellationToken);
        }

        if (operationIds.Count > 0)
        {
            logger.LogInformation("Recovered {Count} pending bulk operation(s).", operationIds.Count);
        }
    }
}
