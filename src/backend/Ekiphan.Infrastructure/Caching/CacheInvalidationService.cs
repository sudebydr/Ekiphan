namespace Ekiphan.Infrastructure.Caching;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ekiphan.Application.Caching;

public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly IApplicationCache _cache;

    public CacheInvalidationService(IApplicationCache cache)
    {
        _cache = cache;
    }

    public async Task InvalidateByTagAsync(string tag, string reason, CancellationToken cancellationToken)
    {
        // Could log the reason here
        await _cache.RemoveByTagAsync(tag, cancellationToken);
    }

    public async Task InvalidateByTagsAsync(IEnumerable<string> tags, string reason, CancellationToken cancellationToken)
    {
        foreach (var tag in tags)
        {
            await InvalidateByTagAsync(tag, reason, cancellationToken);
        }
    }
}
