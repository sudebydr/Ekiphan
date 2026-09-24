namespace Ekiphan.Infrastructure.Metrics;

using System.Diagnostics.Metrics;
using Ekiphan.Application.Metrics;

public class CacheMetrics : ICacheMetrics
{
    private readonly Counter<long> _hitsCounter;
    private readonly Counter<long> _missesCounter;
    private readonly Counter<long> _setsCounter;
    private readonly Counter<long> _errorsCounter;

    public CacheMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Ekiphan.Cache");
        _hitsCounter = meter.CreateCounter<long>("cache.hits");
        _missesCounter = meter.CreateCounter<long>("cache.misses");
        _setsCounter = meter.CreateCounter<long>("cache.sets");
        _errorsCounter = meter.CreateCounter<long>("cache.errors");
    }

    public void RecordHit() => _hitsCounter.Add(1);
    public void RecordMiss() => _missesCounter.Add(1);
    public void RecordSet() => _setsCounter.Add(1);
    public void RecordRemove() { } // Optional mapping
    public void RecordError() => _errorsCounter.Add(1);
    public void RecordStampedeLockWait(double milliseconds) { }
    public void RecordStaleResponse() { }
}
