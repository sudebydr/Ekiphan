using Ekiphan.Application.Quotes;
using Ekiphan.Domain.Quotes;
using Ekiphan.Infrastructure.Persistence;
using Ekiphan.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Quotes;

internal sealed class QuoteManagementRepository(EkiphanDbContext dbContext)
    : IQuoteManagementRepository
{
    public Task<QuoteRequest?> GetAsync(
        Guid quoteId,
        CancellationToken cancellationToken = default) =>
        dbContext.QuoteRequests
            .Include(quote => quote.StatusHistory)
            .Include(quote => quote.InternalNotes)
            .SingleOrDefaultAsync(
                quote => quote.Id == quoteId,
                cancellationToken);

    public Task<bool> CanAssignUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.AdminUsers.AnyAsync(
            user =>
                user.Id == userId &&
                user.IsActive &&
                user.Permissions.Any(grant =>
                    grant.Permission == AdminPermissionCode.QuotesRead ||
                    grant.Permission == AdminPermissionCode.QuotesManage),
            cancellationToken);

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new QuoteConcurrencyException();
        }
    }
}
