using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Content;

public sealed class ContactRequestNote : Entity
{
    private ContactRequestNote()
    {
    }

    internal ContactRequestNote(
        Guid id,
        Guid contactRequestId,
        Guid authorUserId,
        string text,
        DateTimeOffset recordedAt) : base(id)
    {
        if (contactRequestId == Guid.Empty || authorUserId == Guid.Empty)
        {
            throw new ArgumentException("Identifiers cannot be empty.");
        }

        ContactRequestId = contactRequestId;
        AuthorUserId = authorUserId;
        Text = Required(text, 2000, nameof(text));
        RecordedAt = recordedAt == default
            ? throw new ArgumentException(
                "Recorded timestamp is required.",
                nameof(recordedAt))
            : recordedAt;
    }

    public Guid ContactRequestId { get; private set; }

    public Guid AuthorUserId { get; private set; }

    public string Text { get; private set; } = string.Empty;

    public DateTimeOffset RecordedAt { get; private set; }

    private static string Required(string value, int maximum, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        var normalized = value.Trim();
        return normalized.Length <= maximum
            ? normalized
            : throw new ArgumentOutOfRangeException(name);
    }
}
