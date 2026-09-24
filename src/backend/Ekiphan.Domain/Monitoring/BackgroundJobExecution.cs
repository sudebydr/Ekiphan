namespace Ekiphan.Domain.Monitoring;

using System;
using Ekiphan.Domain.Common;

public sealed class BackgroundJobExecution : Entity
{
    public string JobType { get; private set; } = default!;
    public string JobKey { get; private set; } = default!;
    public string? CorrelationId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public BackgroundJobExecutionStatus Status { get; private set; }
    public int AttemptNumber { get; private set; }
    public double? DurationMs { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? InstanceId { get; private set; }
    public string? MetadataJson { get; private set; }

    private BackgroundJobExecution() { }

    public BackgroundJobExecution(
        Guid id,
        string jobType,
        string jobKey,
        string? correlationId,
        DateTimeOffset startedAt,
        int attemptNumber,
        string? instanceId)
        : base(id)
    {
        JobType = jobType;
        JobKey = jobKey;
        CorrelationId = correlationId;
        StartedAt = startedAt;
        Status = BackgroundJobExecutionStatus.Running;
        AttemptNumber = attemptNumber;
        InstanceId = instanceId;
    }

    public void Complete(DateTimeOffset completedAt, double durationMs, string? metadataJson)
    {
        CompletedAt = completedAt;
        DurationMs = durationMs;
        Status = BackgroundJobExecutionStatus.Completed;
        MetadataJson = metadataJson;
    }

    public void Fail(DateTimeOffset failedAt, double durationMs, string errorCode, string errorMessage)
    {
        CompletedAt = failedAt;
        DurationMs = durationMs;
        Status = BackgroundJobExecutionStatus.Failed;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }
    
    public void MarkAsDeadLetter()
    {
        Status = BackgroundJobExecutionStatus.DeadLetter;
    }
}
