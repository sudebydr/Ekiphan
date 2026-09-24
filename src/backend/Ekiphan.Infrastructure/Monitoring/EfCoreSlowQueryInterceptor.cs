namespace Ekiphan.Infrastructure.Monitoring;

using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Ekiphan.Application.Metrics;
using Ekiphan.Application.Monitoring;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

public class EfCoreSlowQueryInterceptor : DbCommandInterceptor
{
    private const int SlowQueryThresholdMs = 500;
    private readonly ILogger<EfCoreSlowQueryInterceptor> _logger;
    private readonly ISlowQueryMonitor _slowQueryMonitor;
    private readonly IPerformanceMetrics _performanceMetrics;

    public EfCoreSlowQueryInterceptor(
        ILogger<EfCoreSlowQueryInterceptor> logger,
        ISlowQueryMonitor slowQueryMonitor,
        IPerformanceMetrics performanceMetrics)
    {
        _logger = logger;
        _slowQueryMonitor = slowQueryMonitor;
        _performanceMetrics = performanceMetrics;
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        if (eventData.Duration.TotalMilliseconds > SlowQueryThresholdMs)
        {
            _performanceMetrics.RecordSlowDatabaseQuery();
            await _slowQueryMonitor.LogSlowQueryAsync(command.CommandText, eventData.Duration.TotalMilliseconds, "db", "N/A");
            _logger.LogWarning("Slow query detected ({Duration}ms): {CommandText}", eventData.Duration.TotalMilliseconds, command.CommandText); // Parameter values are intentionally NOT logged for security
        }

        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}
