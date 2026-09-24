namespace Ekiphan.Application.Monitoring;

using System.Threading.Tasks;

public interface IRequestPerformanceMonitor
{
    Task RecordRequestAsync(string method, string pathTemplate, int statusCode, double durationMs, string? userId, string correlationId, bool cacheHit, int databaseQueryCount, long responseSize);
}
