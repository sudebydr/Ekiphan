using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Quotes;

public enum QuoteActivityType
{
    Created = 1,
    Assigned = 2,
    Unassigned = 3,
    StatusChanged = 4,
    NoteAdded = 5,
    NoteUpdated = 6,
    NoteDeleted = 7,
    ExportedCsv = 8,
    ExportedExcel = 9,
    PdfGenerated = 10,
    CustomerEmailQueued = 11,
    CustomerEmailSent = 12,
    AdminEmailQueued = 13,
    AdminEmailSent = 14,
    EmailFailed = 15,
    Viewed = 16,
    Archived = 17,
    DeleteRequested = 18,
    Deleted = 19,
    Anonymized = 20,
    Restored = 21,
}

public sealed class QuoteActivity : Entity
{
    private QuoteActivity()
    {
    }

    public QuoteActivity(
        Guid id,
        Guid quoteRequestId,
        QuoteActivityType activityType,
        Guid? actorUserId,
        string? previousValue,
        string? newValue,
        string description,
        string? metadataJson,
        string? ipAddress,
        string? userAgent,
        string correlationId,
        DateTimeOffset createdAt) : base(id)
    {
        if (quoteRequestId == Guid.Empty)
        {
            throw new ArgumentException("Quote request identifier is required.", nameof(quoteRequestId));
        }

        QuoteRequestId = quoteRequestId;
        ActivityType = activityType;
        ActorUserId = actorUserId;
        PreviousValue = QuoteGuard.Optional(previousValue, 500, nameof(previousValue));
        NewValue = QuoteGuard.Optional(newValue, 500, nameof(newValue));
        Description = QuoteGuard.Required(description, 1000, nameof(description));
        MetadataJson = QuoteGuard.Optional(metadataJson, 4000, nameof(metadataJson));
        IpAddress = QuoteGuard.Optional(ipAddress, 100, nameof(ipAddress));
        UserAgent = QuoteGuard.Optional(userAgent, 500, nameof(userAgent));
        CorrelationId = QuoteGuard.Required(correlationId, 100, nameof(correlationId));
        CreatedAt = createdAt != default ? createdAt : DateTimeOffset.UtcNow;
    }

    public Guid QuoteRequestId { get; private set; }

    public QuoteActivityType ActivityType { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public string? PreviousValue { get; private set; }

    public string? NewValue { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public string? MetadataJson { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public string CorrelationId { get; private set; } = string.Empty;

    public new DateTimeOffset CreatedAt { get; private set; }
}
