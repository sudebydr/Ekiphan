using Ekiphan.Domain.Quotes;

namespace Ekiphan.Application.Quotes;

public interface IAdminQuoteQueryService
{
    Task<AdminQuotePagedResult<AdminQuoteSummary>> GetQuotesAsync(
        AdminQuoteListQuery query,
        CancellationToken cancellationToken = default);

    Task<AdminQuoteDetail?> GetQuoteAsync(
        Guid quoteId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminRequestAssignee>> GetAssigneesAsync(
        CancellationToken cancellationToken = default);
}
