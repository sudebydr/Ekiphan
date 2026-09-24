namespace Ekiphan.Infrastructure.Metrics;

using System.Diagnostics.Metrics;
using Ekiphan.Application.Metrics;

public class PerformanceMetrics : IPerformanceMetrics
{
    private readonly Counter<long> _httpRequestsTotal;
    private readonly Histogram<double> _httpRequestDuration;
    private readonly Counter<long> _slowQueriesTotal;

    public PerformanceMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Ekiphan.Performance");
        _httpRequestsTotal = meter.CreateCounter<long>("http.requests.total");
        _httpRequestDuration = meter.CreateHistogram<double>("http.request.duration_ms");
        _slowQueriesTotal = meter.CreateCounter<long>("db.slow_queries.total");
    }

    public void RecordHttpRequestDuration(string method, string pathTemplate, int statusCode, double durationMs)
    {
        _httpRequestsTotal.Add(1);
        _httpRequestDuration.Record(durationMs);
    }

    public void RecordHttpRequestError() { }
    public void RecordDatabaseQueryDuration(double durationMs) { }
    public void RecordSlowDatabaseQuery() => _slowQueriesTotal.Add(1);
    public void RecordDatabaseConnectionError() { }
    public void RecordHealthCheckFailure() { }
    public void RecordRateLimitRejected() { }
}
