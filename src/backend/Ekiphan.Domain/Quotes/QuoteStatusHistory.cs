using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Quotes;

public sealed class QuoteStatusHistory : Entity
{
    private QuoteStatusHistory()
    {
    }

    internal QuoteStatusHistory(
        Guid id,
        Guid quoteRequestId,
        QuoteStatus? fromStatus,
        QuoteStatus toStatus,
        DateTimeOffset changedAt,
        Guid? changedByUserId,
        string? note)
        : base(id)
    {
        QuoteRequestId = quoteRequestId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedAt = changedAt;
        ChangedByUserId = changedByUserId;
        Note = QuoteGuard.Optional(note, 1000, nameof(note));
    }

    public Guid QuoteRequestId { get; private set; }

    public QuoteStatus? FromStatus { get; private set; }

    public QuoteStatus ToStatus { get; private set; }

    public DateTimeOffset ChangedAt { get; private set; }

    public Guid? ChangedByUserId { get; private set; }

    public string? Note { get; private set; }
}
