using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Quotes;

public enum EmailQueueStatus
{
    Pending = 1,
    Processing = 2,
    Sent = 3,
    Failed = 4,
    DeadLetter = 5,
    Cancelled = 6,
}

public sealed class EmailQueueItem : Entity
{
    private EmailQueueItem()
    {
    }

    public EmailQueueItem(
        Guid id,
        string relatedEntityType,
        Guid relatedEntityId,
        string recipient,
        string subject,
        string templateCode,
        string templateDataJson,
        string correlationId,
        DateTimeOffset createdAt,
        int maxAttempts = 5) : base(id)
    {
        RelatedEntityType = QuoteGuard.Required(relatedEntityType, 100, nameof(relatedEntityType));
        RelatedEntityId = relatedEntityId;
        Recipient = QuoteGuard.Email(recipient);
        Subject = QuoteGuard.Required(subject, 300, nameof(subject));
        TemplateCode = QuoteGuard.Required(templateCode, 100, nameof(templateCode));
        TemplateDataJson = QuoteGuard.Required(templateDataJson, 8000, nameof(templateDataJson));
        CorrelationId = QuoteGuard.Required(correlationId, 100, nameof(correlationId));
        CreatedAt = createdAt != default ? createdAt : DateTimeOffset.UtcNow;
        MaxAttempts = Math.Max(1, maxAttempts);
        Status = EmailQueueStatus.Pending;
        NextAttemptAt = CreatedAt;
    }

    public string RelatedEntityType { get; private set; } = string.Empty;

    public Guid RelatedEntityId { get; private set; }

    public string Recipient { get; private set; } = string.Empty;

    public string Subject { get; private set; } = string.Empty;

    public string TemplateCode { get; private set; } = string.Empty;

    public string TemplateDataJson { get; private set; } = string.Empty;

    public EmailQueueStatus Status { get; private set; } = EmailQueueStatus.Pending;

    public int AttemptCount { get; private set; }

    public int MaxAttempts { get; private set; } = 5;

    public DateTimeOffset? NextAttemptAt { get; private set; }

    public DateTimeOffset? LastAttemptAt { get; private set; }

    public DateTimeOffset? SentAt { get; private set; }

    public string? LastErrorCode { get; private set; }

    public string? LastErrorMessage { get; private set; }

    public string CorrelationId { get; private set; } = string.Empty;

    public new DateTimeOffset CreatedAt { get; private set; }

    public void MarkProcessing(DateTimeOffset attemptedAt)
    {
        Status = EmailQueueStatus.Processing;
        LastAttemptAt = attemptedAt;
        AttemptCount++;
    }

    public void MarkSent(DateTimeOffset sentAt)
    {
        Status = EmailQueueStatus.Sent;
        SentAt = sentAt;
        NextAttemptAt = null;
        LastErrorCode = null;
        LastErrorMessage = null;
    }

    public void MarkFailed(DateTimeOffset failedAt, string errorCode, string errorMessage, TimeSpan? retryDelay = null)
    {
        LastErrorCode = QuoteGuard.Optional(errorCode, 100, nameof(errorCode));
        LastErrorMessage = QuoteGuard.Optional(errorMessage, 1000, nameof(errorMessage));

        if (AttemptCount >= MaxAttempts || retryDelay == null)
        {
            Status = EmailQueueStatus.DeadLetter;
            NextAttemptAt = null;
        }
        else
        {
            Status = EmailQueueStatus.Failed;
            NextAttemptAt = failedAt.Add(retryDelay.Value);
        }
    }

    public void RequestManualRetry(DateTimeOffset requestedAt)
    {
        if (Status != EmailQueueStatus.Failed && Status != EmailQueueStatus.DeadLetter)
        {
            throw new InvalidOperationException("Only failed or dead-letter email queue items can be retried.");
        }

        Status = EmailQueueStatus.Pending;
        NextAttemptAt = requestedAt;
        LastErrorCode = null;
        LastErrorMessage = null;
    }
}
