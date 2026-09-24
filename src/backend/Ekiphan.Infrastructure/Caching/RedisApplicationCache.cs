namespace Ekiphan.Infrastructure.Caching;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ekiphan.Application.Caching;
using StackExchange.Redis;

public class RedisApplicationCache : IApplicationCache
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    
    public RedisApplicationCache(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        var value = await _db.StringGetAsync(key);
        if (!value.HasValue)
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(value!);
        }
        catch
        {
            await _db.KeyDeleteAsync(key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken cancellationToken)
    {
        var serializedValue = JsonSerializer.Serialize(value);
        var expiry = options.AbsoluteExpiration ?? options.SlidingExpiration;
        if (expiry.HasValue)
        {
            await _db.StringSetAsync(key, serializedValue, expiry.Value);
        }
        else
        {
            await _db.StringSetAsync(key, serializedValue);
        }

        if (options.Tags != null && options.Tags.Count > 0)
        {
            var batch = _db.CreateBatch();
            foreach (var tag in options.Tags)
            {
                var tagKey = $"tag:{tag}";
                _ = batch.SetAddAsync(tagKey, key);
                if (options.AbsoluteExpiration.HasValue)
                {
                    _ = batch.KeyExpireAsync(tagKey, options.AbsoluteExpiration.Value.Add(TimeSpan.FromDays(1))); // Keep tag alive a bit longer
                }
            }
            batch.Execute();
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken)
    {
        var tagKey = $"tag:{tag}";
        var members = await _db.SetMembersAsync(tagKey);
        
        if (members.Length > 0)
        {
            var keysToDelete = new RedisKey[members.Length + 1];
            for (int i = 0; i < members.Length; i++)
            {
                keysToDelete[i] = members[i].ToString();
            }
            keysToDelete[members.Length] = tagKey;
            
            await _db.KeyDeleteAsync(keysToDelete);
        }
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CacheEntryOptions options, CancellationToken cancellationToken)
    {
        var cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        var factoryValue = await factory(cancellationToken);
        await SetAsync(key, factoryValue, options, cancellationToken);
        return factoryValue;
    }
}
