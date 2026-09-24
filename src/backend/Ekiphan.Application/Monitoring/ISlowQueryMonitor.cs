namespace Ekiphan.Application.Monitoring;

using System.Threading.Tasks;

public interface ISlowQueryMonitor
{
    Task LogSlowQueryAsync(string commandText, double durationMs, string endpoint, string correlationId);
}
