namespace Ekiphan.Application.Caching;

using System;
using System.Collections.Generic;

public class CacheEntryOptions
{
    public TimeSpan? AbsoluteExpiration { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool UseCompression { get; set; }
    public bool AllowStale { get; set; }
    public TimeSpan? StaleDuration { get; set; }
    public TimeSpan? LockTimeout { get; set; }
    public string SerializerVersion { get; set; } = "v1";
}
