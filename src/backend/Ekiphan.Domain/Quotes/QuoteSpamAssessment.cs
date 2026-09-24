using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Quotes;

public enum SpamRecommendedAction
{
    Allow = 1,
    AllowAndFlag = 2,
    RequireReview = 3,
    Reject = 4,
}

public sealed class QuoteSpamAssessment : Entity
{
    private QuoteSpamAssessment()
    {
    }

    public QuoteSpamAssessment(
        Guid id,
        Guid quoteRequestId,
        int riskScore,
        string reasonsJson,
        SpamRecommendedAction recommendedAction,
        DateTimeOffset createdAt) : base(id)
    {
        if (quoteRequestId == Guid.Empty)
        {
            throw new ArgumentException("Quote request identifier is required.", nameof(quoteRequestId));
        }

        QuoteRequestId = quoteRequestId;
        RiskScore = Math.Clamp(riskScore, 0, 100);
        ReasonsJson = QuoteGuard.Required(reasonsJson, 2000, nameof(reasonsJson));
        RecommendedAction = recommendedAction;
        CreatedAt = createdAt != default ? createdAt : DateTimeOffset.UtcNow;
    }

    public Guid QuoteRequestId { get; private set; }

    public int RiskScore { get; private set; }

    public string ReasonsJson { get; private set; } = string.Empty;

    public SpamRecommendedAction RecommendedAction { get; private set; }

    public new DateTimeOffset CreatedAt { get; private set; }
}
