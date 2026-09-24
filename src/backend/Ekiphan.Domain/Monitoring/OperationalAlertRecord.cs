namespace Ekiphan.Domain.Monitoring;

using System;
using Ekiphan.Domain.Common;

public sealed class OperationalAlertRecord : Entity
{
    public string AlertType { get; private set; } = default!;
    public OperationalAlertSeverity Severity { get; private set; }
    public string Title { get; private set; } = default!;
    public string Message { get; private set; } = default!;
    public string Fingerprint { get; private set; } = default!;
    public DateTimeOffset FirstDetectedAt { get; private set; }
    public DateTimeOffset LastDetectedAt { get; private set; }
    public DateTimeOffset? LastSentAt { get; private set; }
    public int OccurrenceCount { get; private set; }
    public OperationalAlertStatus Status { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public DateTimeOffset? AcknowledgedAt { get; private set; }
    public Guid? AcknowledgedByUserId { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? MetadataJson { get; private set; }
    public DateTimeOffset? SuppressedUntil { get; private set; }

    private OperationalAlertRecord() { }

    public OperationalAlertRecord(
        Guid id,
        string alertType,
        OperationalAlertSeverity severity,
        string title,
        string message,
        string fingerprint,
        DateTimeOffset detectedAt,
        string? correlationId,
        string? metadataJson)
        : base(id)
    {
        AlertType = alertType;
        Severity = severity;
        Title = title;
        Message = message;
        Fingerprint = fingerprint;
        FirstDetectedAt = detectedAt;
        LastDetectedAt = detectedAt;
        OccurrenceCount = 1;
        Status = OperationalAlertStatus.Active;
        CorrelationId = correlationId;
        MetadataJson = metadataJson;
    }

    public void RecordOccurrence(DateTimeOffset detectedAt)
    {
        LastDetectedAt = detectedAt;
        OccurrenceCount++;
        if (Status == OperationalAlertStatus.Resolved)
        {
            Status = OperationalAlertStatus.Active;
        }
    }

    public void MarkAsSent(DateTimeOffset sentAt)
    {
        LastSentAt = sentAt;
    }

    public void Acknowledge(Guid userId, DateTimeOffset acknowledgedAt)
    {
        AcknowledgedByUserId = userId;
        AcknowledgedAt = acknowledgedAt;
        Status = OperationalAlertStatus.Acknowledged;
    }

    public void Resolve(DateTimeOffset resolvedAt)
    {
        ResolvedAt = resolvedAt;
        Status = OperationalAlertStatus.Resolved;
    }

    public void Suppress(DateTimeOffset suppressedUntil)
    {
        SuppressedUntil = suppressedUntil;
        Status = OperationalAlertStatus.Suppressed;
    }
}
