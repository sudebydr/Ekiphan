using Ekiphan.Domain.Quotes;

namespace Ekiphan.Application.Quotes;

public interface IQuoteManagementRepository
{
    Task<QuoteRequest?> GetAsync(
        Guid quoteId,
        CancellationToken cancellationToken = default);

    Task<bool> CanAssignUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
