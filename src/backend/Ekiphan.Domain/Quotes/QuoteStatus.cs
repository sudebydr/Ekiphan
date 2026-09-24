namespace Ekiphan.Domain.Quotes;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1069:Enums should not have duplicate values", Justification = "Backwards compatibility aliases for quote status workflow.")]
public enum QuoteStatus
{
    New = 1,
    Reviewing = 2,
    InReview = 2,
    Assigned = 10,
    Contacted = 3,
    Preparing = 4,
    Sent = 5,
    Quoted = 5,
    Won = 6,
    Approved = 6,
    Lost = 7,
    Rejected = 7,
    Cancelled = 11,
    Archived = 8,
}
