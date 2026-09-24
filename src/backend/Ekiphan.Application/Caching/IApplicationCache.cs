namespace Ekiphan.Application.Caching;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface IApplicationCache
{
    Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken);

    Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions options,
        CancellationToken cancellationToken);

    Task RemoveAsync(
        string key,
        CancellationToken cancellationToken);

    Task RemoveByTagAsync(
        string tag,
        CancellationToken cancellationToken);

    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheEntryOptions options,
        CancellationToken cancellationToken);
}
