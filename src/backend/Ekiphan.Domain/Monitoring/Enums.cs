namespace Ekiphan.Domain.Monitoring;

public enum BackgroundJobExecutionStatus
{
    Running,
    Completed,
    Failed,
    Retrying,
    Cancelled,
    DeadLetter
}

public enum OperationalAlertStatus
{
    Active,
    Acknowledged,
    Resolved,
    Suppressed
}

public enum OperationalAlertSeverity
{
    Critical,
    High,
    Medium,
    Low
}
