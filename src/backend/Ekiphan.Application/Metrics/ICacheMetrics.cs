namespace Ekiphan.Application.Metrics;

public interface ICacheMetrics
{
    void RecordHit();
    void RecordMiss();
    void RecordSet();
    void RecordRemove();
    void RecordError();
    void RecordStampedeLockWait(double milliseconds);
    void RecordStaleResponse();
}
