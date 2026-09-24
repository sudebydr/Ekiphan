namespace Ekiphan.Infrastructure.Caching;

using System;
using System.Threading;
using System.Threading.Tasks;
using Ekiphan.Application.Caching;
using StackExchange.Redis;

public class RedisDistributedLockService : IDistributedLockService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly string _lockValue;

    public RedisDistributedLockService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = redis.GetDatabase();
        _lockValue = Guid.NewGuid().ToString(); // Unique per instance
    }

    public async Task<bool> TryAcquireLockAsync(string lockKey, TimeSpan expiration, CancellationToken cancellationToken)
    {
        return await _db.StringSetAsync(lockKey, _lockValue, expiration, When.NotExists);
    }

    public async Task ReleaseLockAsync(string lockKey, CancellationToken cancellationToken)
    {
        var currentValue = await _db.StringGetAsync(lockKey);
        if (currentValue == _lockValue)
        {
            await _db.KeyDeleteAsync(lockKey);
        }
    }
}
