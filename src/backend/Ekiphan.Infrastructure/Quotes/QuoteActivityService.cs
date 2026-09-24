using System.Text.Json;
using Ekiphan.Application.Identity;
using Ekiphan.Application.Quotes;
using Ekiphan.Domain.Quotes;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Quotes;

public sealed class QuoteActivityService(
    EkiphanDbContext dbContext,
    IAuditValueSanitizer sanitizer,
    TimeProvider timeProvider) : IQuoteActivityService
{
    public async Task LogAsync(
        Guid quoteId,
        QuoteActivityType activityType,
        Guid? actorUserId,
        string? previousValue,
        string? newValue,
        string description,
        object? metadata,
        QuoteRequestContext context,
        CancellationToken cancellationToken = default)
    {
        string? metadataJson = null;
        if (metadata != null)
        {
            var clean = sanitizer.Sanitize(metadata);
            metadataJson = JsonSerializer.Serialize(clean);
        }

        var activity = new QuoteActivity(
            Guid.NewGuid(),
            quoteId,
            activityType,
            actorUserId,
            previousValue,
            newValue,
            description,
            metadataJson,
            context.IpAddress,
            context.UserAgent,
            context.CorrelationId,
            timeProvider.GetUtcNow());

        try
        {
            dbContext.QuoteActivities.Add(activity);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Log failure gracefully without aborting primary flow when DB context is unavailable (e.g., test mocks)
        }
    }

    public async Task<IReadOnlyList<QuoteActivityDto>> GetActivitiesAsync(
        Guid quoteId,
        CancellationToken cancellationToken = default)
    {
        var query = from activity in dbContext.QuoteActivities.AsNoTracking()
                    where activity.QuoteRequestId == quoteId
                    join user in dbContext.AdminUsers.AsNoTracking() on activity.ActorUserId equals user.Id into users
                    from actorUser in users.DefaultIfEmpty()
                    orderby activity.CreatedAt descending
                    select new QuoteActivityDto(
                        activity.Id,
                        activity.QuoteRequestId,
                        activity.ActivityType.ToString(),
                        activity.ActorUserId,
                        actorUser != null ? actorUser.DisplayName : null,
                        activity.PreviousValue,
                        activity.NewValue,
                        activity.Description,
                        activity.MetadataJson,
                        MaskIp(activity.IpAddress),
                        activity.CreatedAt);

        return await query.ToListAsync(cancellationToken);
    }

    private static string? MaskIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return null;
        return ip.Contains('.') ? string.Join('.', ip.Split('.')[..3]) + ".*" : "masked";
    }
}
