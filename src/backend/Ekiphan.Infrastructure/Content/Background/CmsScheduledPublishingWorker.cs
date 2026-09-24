using Ekiphan.Application.Content;
using Ekiphan.Domain.Content;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ekiphan.Infrastructure.Content.Background;

public sealed class CmsScheduledPublishingService(IServiceProvider serviceProvider) : ICmsScheduledPublishingService
{
    public async Task ExecuteScheduledPublishingAsync(CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EkiphanDbContext>();
        var workflowService = scope.ServiceProvider.GetRequiredService<IContentWorkflowService>();

        var now = DateTimeOffset.UtcNow;

        // Auto-publish scheduled ReferenceProjects
        var refsToPublish = await dbContext.Set<ReferenceProject>()
            .Where(r => r.WorkflowStatus == ContentWorkflowStatus.Scheduled && r.PublishAt != null && r.PublishAt <= now)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        foreach (var id in refsToPublish)
        {
            await workflowService.TransitionAsync("ReferenceProject", id, ContentWorkflowStatus.Published, null, "Auto-published by scheduled job.", Array.Empty<byte>(), Guid.Empty, cancellationToken);
        }

        // Auto-publish scheduled Showrooms
        var showroomsToPublish = await dbContext.Set<Showroom>()
            .Where(s => s.WorkflowStatus == ContentWorkflowStatus.Scheduled && s.PublishAt != null && s.PublishAt <= now)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        foreach (var id in showroomsToPublish)
        {
            await workflowService.TransitionAsync("Showroom", id, ContentWorkflowStatus.Published, null, "Auto-published by scheduled job.", Array.Empty<byte>(), Guid.Empty, cancellationToken);
        }

        // Auto-publish scheduled Banners
        var bannersToPublish = await dbContext.Set<Banner>()
            .Where(b => b.WorkflowStatus == ContentWorkflowStatus.Scheduled && b.PublishAt != null && b.PublishAt <= now)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        foreach (var id in bannersToPublish)
        {
            await workflowService.TransitionAsync("Banner", id, ContentWorkflowStatus.Published, null, "Auto-published by scheduled job.", Array.Empty<byte>(), Guid.Empty, cancellationToken);
        }
    }

    public async Task ExecuteBannerExpirationAsync(CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EkiphanDbContext>();
        var workflowService = scope.ServiceProvider.GetRequiredService<IContentWorkflowService>();

        var now = DateTimeOffset.UtcNow;

        // Auto-unpublish expired Banners
        var expiredBanners = await dbContext.Set<Banner>()
            .Where(b => b.WorkflowStatus == ContentWorkflowStatus.Published && b.PublishEndAt != null && b.PublishEndAt <= now)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        foreach (var id in expiredBanners)
        {
            await workflowService.TransitionAsync("Banner", id, ContentWorkflowStatus.Unpublished, null, "Auto-unpublished due to expiration.", Array.Empty<byte>(), Guid.Empty, cancellationToken);
        }
    }
}

public sealed class CmsScheduledPublishingWorker(
    ICmsScheduledPublishingService scheduledService,
    ILogger<CmsScheduledPublishingWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("CmsScheduledPublishingWorker started.");

        using var timer = new PeriodicTimer(Interval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await scheduledService.ExecuteScheduledPublishingAsync(stoppingToken);
                await scheduledService.ExecuteBannerExpirationAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in CmsScheduledPublishingWorker.");
            }
        }
    }
}
