namespace Ekiphan.Infrastructure.Caching;

using System.Collections.Concurrent;
using Ekiphan.Application.Caching;

public sealed class InMemoryDistributedLockService : IDistributedLockService
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> locks =
        new(StringComparer.Ordinal);

    public Task<bool> TryAcquireLockAsync(
        string lockKey,
        TimeSpan expiration,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(expiration);

        while (true)
        {
            if (locks.TryAdd(lockKey, expiresAt))
                return Task.FromResult(true);

            if (!locks.TryGetValue(lockKey, out var current))
                continue;

            if (current > now)
                return Task.FromResult(false);

            if (locks.TryUpdate(lockKey, expiresAt, current))
                return Task.FromResult(true);
        }
    }

    public Task ReleaseLockAsync(
        string lockKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        locks.TryRemove(lockKey, out _);
        return Task.CompletedTask;
    }
}
