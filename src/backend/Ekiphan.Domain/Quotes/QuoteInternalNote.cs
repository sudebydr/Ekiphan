using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Quotes;

public sealed class QuoteInternalNote : Entity
{
    private QuoteInternalNote()
    {
    }

    internal QuoteInternalNote(
        Guid id,
        Guid quoteRequestId,
        Guid authorUserId,
        string text,
        DateTimeOffset createdAt) : base(id)
    {
        if (quoteRequestId == Guid.Empty || authorUserId == Guid.Empty)
        {
            throw new ArgumentException("Identifiers cannot be empty.");
        }

        if (createdAt == default)
        {
            throw new ArgumentException(
                "Created timestamp is required.",
                nameof(createdAt));
        }

        QuoteRequestId = quoteRequestId;
        AuthorUserId = authorUserId;
        Text = QuoteGuard.Required(text, 2000, nameof(text));
        RecordedAt = createdAt;
    }

    public Guid QuoteRequestId { get; private set; }

    public Guid AuthorUserId { get; private set; }

    public string Text { get; private set; } = string.Empty;

    public DateTimeOffset RecordedAt { get; private set; }

    public new DateTimeOffset? UpdatedAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public Guid? DeletedByUserId { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public void Update(string newText, DateTimeOffset updatedAt)
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("Cannot update a deleted note.");
        }

        Text = QuoteGuard.Required(newText, 2000, nameof(newText));
        UpdatedAt = updatedAt != default ? updatedAt : DateTimeOffset.UtcNow;
    }

    public void SoftDelete(Guid deletedByUserId, DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedByUserId = deletedByUserId;
        DeletedAt = deletedAt != default ? deletedAt : DateTimeOffset.UtcNow;
    }
}
