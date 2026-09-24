namespace Ekiphan.Domain.Monitoring;

using System;
using Ekiphan.Domain.Common;

public sealed class ErrorAggregate : Entity
{
    public string ErrorCode { get; private set; } = default!;
    public string ExceptionType { get; private set; } = default!;
    public string MessageSummary { get; private set; } = default!;
    public string PathTemplate { get; private set; } = default!;
    public int OccurrenceCount { get; private set; }
    public DateTimeOffset FirstSeenAt { get; private set; }
    public DateTimeOffset LastSeenAt { get; private set; }
    public string? LatestCorrelationId { get; private set; }
    public OperationalAlertSeverity Severity { get; private set; }

    private ErrorAggregate() { }

    public ErrorAggregate(
        Guid id,
        string errorCode,
        string exceptionType,
        string messageSummary,
        string pathTemplate,
        DateTimeOffset seenAt,
        string? correlationId,
        OperationalAlertSeverity severity)
        : base(id)
    {
        ErrorCode = errorCode;
        ExceptionType = exceptionType;
        MessageSummary = messageSummary;
        PathTemplate = pathTemplate;
        FirstSeenAt = seenAt;
        LastSeenAt = seenAt;
        LatestCorrelationId = correlationId;
        Severity = severity;
        OccurrenceCount = 1;
    }

    public void RecordOccurrence(DateTimeOffset seenAt, string? correlationId)
    {
        LastSeenAt = seenAt;
        LatestCorrelationId = correlationId ?? LatestCorrelationId;
        OccurrenceCount++;
    }
}
