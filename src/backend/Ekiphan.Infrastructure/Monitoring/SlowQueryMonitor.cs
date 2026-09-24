namespace Ekiphan.Infrastructure.Monitoring;

using System.Threading.Tasks;
using Ekiphan.Application.Monitoring;
using Microsoft.Extensions.Logging;

public class SlowQueryMonitor : ISlowQueryMonitor
{
    private readonly ILogger<SlowQueryMonitor> _logger;

    public SlowQueryMonitor(ILogger<SlowQueryMonitor> logger)
    {
        _logger = logger;
    }

    public Task LogSlowQueryAsync(string commandText, double durationMs, string endpoint, string correlationId)
    {
        _logger.LogWarning("Slow Query [{Duration}ms] | Correlation: {CorrelationId} | Endpoint: {Endpoint} | Command: {CommandText}", 
            durationMs, correlationId, endpoint, commandText);
        return Task.CompletedTask;
    }
}
