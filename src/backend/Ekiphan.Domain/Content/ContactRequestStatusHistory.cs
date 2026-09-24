using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Content;

public sealed class ContactRequestStatusHistory : Entity
{
    private ContactRequestStatusHistory()
    {
    }

    internal ContactRequestStatusHistory(
        Guid id,
        Guid contactRequestId,
        ContactRequestStatus? fromStatus,
        ContactRequestStatus toStatus,
        Guid? changedByUserId,
        DateTimeOffset changedAt) : base(id)
    {
        ContactRequestId = contactRequestId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedByUserId = changedByUserId;
        ChangedAt = changedAt;
    }

    public Guid ContactRequestId { get; private set; }

    public ContactRequestStatus? FromStatus { get; private set; }

    public ContactRequestStatus ToStatus { get; private set; }

    public Guid? ChangedByUserId { get; private set; }

    public DateTimeOffset ChangedAt { get; private set; }
}
