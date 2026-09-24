using Ekiphan.Application.Catalog;
using Microsoft.Extensions.Logging;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Standard logging is sufficient.")]

namespace Ekiphan.Infrastructure.Catalog;

public sealed class ProductCacheInvalidationService(ILogger<ProductCacheInvalidationService> logger)
    : IProductCacheInvalidationService
{
    public Task InvalidateProductCacheAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Product cache invalidated for product {ProductId}.", productId);
        return Task.CompletedTask;
    }

    public Task InvalidateCategoryCacheAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Category cache invalidated for category {CategoryId}.", categoryId);
        return Task.CompletedTask;
    }

    public Task InvalidateBrandCacheAsync(Guid? brandId, CancellationToken cancellationToken = default)
    {
        if (brandId.HasValue && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Brand cache invalidated for brand {BrandId}.", brandId.Value);
        }
        return Task.CompletedTask;
    }
}

public sealed class ProductSearchIndexService(ILogger<ProductSearchIndexService> logger)
    : IProductSearchIndexService
{
    public Task QueueIndexUpdateAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Search index update queued for product {ProductId}.", productId);
        return Task.CompletedTask;
    }
}
