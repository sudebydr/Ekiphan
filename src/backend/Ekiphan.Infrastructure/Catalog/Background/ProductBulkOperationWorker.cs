using Ekiphan.Application.Catalog;
using Ekiphan.Domain.Catalog;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Catalog.Background;

public sealed partial class ProductBulkOperationWorker(
    IServiceProvider serviceProvider,
    IProductBulkOperationQueue queue,
    ILogger<ProductBulkOperationWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerStarted(logger);
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        await RecoverPendingOperationsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (queue is InMemoryProductBulkOperationQueue inMemoryQueue && inMemoryQueue.TryDequeue(out var operationId))
                {
                    LogProcessingBulkOperation(logger, operationId);
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
                LogQueueItemProcessingError(logger, ex);
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
            LogRecoveredPendingOperations(logger, operationIds.Count);
        }
    }

    [LoggerMessage(LogLevel.Information, "ProductBulkOperationWorker started.")]
    private static partial void LogWorkerStarted(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Processing bulk operation {OperationId}.")]
    private static partial void LogProcessingBulkOperation(ILogger logger, Guid operationId);

    [LoggerMessage(LogLevel.Error, "Error processing bulk operation queue item.")]
    private static partial void LogQueueItemProcessingError(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Information, "Recovered {Count} pending bulk operation(s).")]
    private static partial void LogRecoveredPendingOperations(ILogger logger, int count);
}
