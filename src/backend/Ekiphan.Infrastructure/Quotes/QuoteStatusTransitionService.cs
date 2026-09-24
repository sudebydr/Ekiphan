using Ekiphan.Application.Quotes;
using Ekiphan.Domain.Quotes;

namespace Ekiphan.Infrastructure.Quotes;

public sealed class QuoteStatusTransitionService : IQuoteStatusTransitionService
{
    private static readonly Dictionary<QuoteStatus, QuoteStatus[]> AllowedTransitions = new()
    {
        [QuoteStatus.New] = [QuoteStatus.InReview, QuoteStatus.Assigned, QuoteStatus.Cancelled],
        [QuoteStatus.InReview] = [QuoteStatus.Assigned, QuoteStatus.Contacted, QuoteStatus.Quoted, QuoteStatus.Cancelled],
        [QuoteStatus.Assigned] = [QuoteStatus.Contacted, QuoteStatus.Quoted, QuoteStatus.Cancelled],
        [QuoteStatus.Contacted] = [QuoteStatus.Quoted, QuoteStatus.Approved, QuoteStatus.Rejected, QuoteStatus.Cancelled],
        [QuoteStatus.Quoted] = [QuoteStatus.Approved, QuoteStatus.Rejected, QuoteStatus.Cancelled],
        [QuoteStatus.Approved] = [QuoteStatus.Archived],
        [QuoteStatus.Rejected] = [QuoteStatus.Archived],
        [QuoteStatus.Cancelled] = [QuoteStatus.Archived],
        [QuoteStatus.Archived] = [],
    };

    public bool CanTransition(QuoteStatus currentStatus, QuoteStatus targetStatus)
    {
        if (currentStatus == targetStatus)
        {
            return true;
        }

        return AllowedTransitions.TryGetValue(currentStatus, out var targets) && targets.Contains(targetStatus);
    }

    public IReadOnlyCollection<QuoteStatus> GetAllowedTransitions(QuoteStatus currentStatus)
    {
        return AllowedTransitions.TryGetValue(currentStatus, out var targets) ? targets : [];
    }
}
