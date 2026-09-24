namespace Ekiphan.Application.Metrics;

public interface IPerformanceMetrics
{
    void RecordHttpRequestDuration(string method, string pathTemplate, int statusCode, double durationMs);
    void RecordHttpRequestError();
    void RecordDatabaseQueryDuration(double durationMs);
    void RecordSlowDatabaseQuery();
    void RecordDatabaseConnectionError();
    void RecordHealthCheckFailure();
    void RecordRateLimitRejected();
}
