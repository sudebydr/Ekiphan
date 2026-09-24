using Ekiphan.Domain.Quotes;

namespace Ekiphan.Application.Quotes;

public interface IQuoteSubmissionRepository
{
    Task<IReadOnlyDictionary<Guid, QuoteProductSnapshot>>
        GetProductSnapshotsAsync(
            IReadOnlyCollection<Guid> productIds,
            string languageCode,
            CancellationToken cancellationToken = default);

    void Add(QuoteRequest quoteRequest);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
