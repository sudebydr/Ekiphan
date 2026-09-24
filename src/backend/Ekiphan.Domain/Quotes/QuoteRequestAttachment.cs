namespace Ekiphan.Domain.Quotes;

public sealed class QuoteRequestAttachment
{
    private QuoteRequestAttachment()
    {
    }

    internal QuoteRequestAttachment(Guid quoteRequestId, Guid mediaAssetId)
    {
        QuoteRequestId = quoteRequestId;
        MediaAssetId = mediaAssetId;
    }

    public Guid QuoteRequestId { get; private set; }

    public Guid MediaAssetId { get; private set; }
}
