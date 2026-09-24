namespace Ekiphan.Infrastructure.Caching;

using System;
using System.Threading;
using System.Threading.Tasks;
using Ekiphan.Application.Caching;
using Microsoft.Extensions.Caching.Memory;

public class MemoryApplicationCache : IApplicationCache
{
    private readonly IMemoryCache _memoryCache;

    public MemoryApplicationCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        _memoryCache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken cancellationToken)
    {
        var cacheOptions = new MemoryCacheEntryOptions();
        
        if (options.AbsoluteExpiration.HasValue)
        {
            cacheOptions.SetAbsoluteExpiration(options.AbsoluteExpiration.Value);
        }
        else if (options.SlidingExpiration.HasValue)
        {
            cacheOptions.SetSlidingExpiration(options.SlidingExpiration.Value);
        }

        _memoryCache.Set(key, value, cacheOptions);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }

    public Task RemoveByTagAsync(string tag, CancellationToken cancellationToken)
    {
        // IMemoryCache doesn't natively support tags. 
        // In a real memory cache tag implementation, we'd use CancellationChangeTokens.
        // For simplicity as a fallback, we can ignore or implement basic tracking.
        return Task.CompletedTask;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CacheEntryOptions options, CancellationToken cancellationToken)
    {
        if (_memoryCache.TryGetValue(key, out T? cachedValue))
        {
            return cachedValue!;
        }

        var factoryValue = await factory(cancellationToken);
        await SetAsync(key, factoryValue, options, cancellationToken);
        return factoryValue;
    }
}
