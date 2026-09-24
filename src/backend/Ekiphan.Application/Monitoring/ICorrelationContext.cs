namespace Ekiphan.Application.Monitoring;

public interface ICorrelationContext
{
    string CorrelationId { get; }
}
