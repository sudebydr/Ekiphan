using Ekiphan.Application.Content;
using Ekiphan.Domain.Content;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Content;

public sealed class ContentWorkflowService(
    EkiphanDbContext dbContext,
    ICmsRevisionService revisionService)
    : IContentWorkflowService
{
    public bool CanTransition(ContentWorkflowStatus currentStatus, ContentWorkflowStatus targetStatus)
    {
        if (currentStatus == targetStatus) return true;

        return currentStatus switch
        {
            ContentWorkflowStatus.Draft => targetStatus is ContentWorkflowStatus.Scheduled or ContentWorkflowStatus.Published or ContentWorkflowStatus.Archived,
            ContentWorkflowStatus.Scheduled => targetStatus is ContentWorkflowStatus.Draft or ContentWorkflowStatus.Published or ContentWorkflowStatus.Archived,
            ContentWorkflowStatus.Published => targetStatus is ContentWorkflowStatus.Unpublished or ContentWorkflowStatus.Archived,
            ContentWorkflowStatus.Unpublished => targetStatus is ContentWorkflowStatus.Draft or ContentWorkflowStatus.Scheduled or ContentWorkflowStatus.Published or ContentWorkflowStatus.Archived,
            ContentWorkflowStatus.Archived => targetStatus is ContentWorkflowStatus.Draft,
            _ => false
        };
    }

    public async Task<ContentWorkflowResultDto> TransitionAsync(
        string contentType,
        Guid contentId,
        ContentWorkflowStatus targetStatus,
        DateTimeOffset? publishAt,
        string? reason,
        byte[] rowVersion,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var type = contentType.Trim().ToLowerInvariant();

        return type switch
        {
            "reference" or "referenceproject" => await TransitionReferenceAsync(contentId, targetStatus, publishAt, reason, rowVersion, actorUserId, cancellationToken),
            "showroom" => await TransitionShowroomAsync(contentId, targetStatus, publishAt, reason, rowVersion, actorUserId, cancellationToken),
            "banner" => await TransitionBannerAsync(contentId, targetStatus, publishAt, reason, rowVersion, actorUserId, cancellationToken),
            _ => new ContentWorkflowResultDto(false, ContentWorkflowStatus.Draft, null, null, CmsManagementErrorCodes.CmsEntityNotFound)
        };
    }

    private async Task<ContentWorkflowResultDto> TransitionReferenceAsync(Guid id, ContentWorkflowStatus targetStatus, DateTimeOffset? publishAt, string? reason, byte[] rowVersion, Guid actorUserId, CancellationToken cancellationToken)
    {
        var refProj = await dbContext.Set<ReferenceProject>()
            .Include(r => r.Translations)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (refProj is null) return new ContentWorkflowResultDto(false, ContentWorkflowStatus.Draft, null, null, CmsManagementErrorCodes.ReferenceNotFound);

        if (!CanTransition(refProj.WorkflowStatus, targetStatus))
            return new ContentWorkflowResultDto(false, refProj.WorkflowStatus, refProj.PublishedAt, refProj.PublishAt, CmsManagementErrorCodes.CmsWorkflowTransitionInvalid);

        if (targetStatus == ContentWorkflowStatus.Published)
        {
            var tr = refProj.Translations.FirstOrDefault(t => t.LanguageCode == "tr");
            if (tr is null || string.IsNullOrWhiteSpace(tr.Title) || string.IsNullOrWhiteSpace(tr.Slug))
                return new ContentWorkflowResultDto(false, refProj.WorkflowStatus, refProj.PublishedAt, refProj.PublishAt, CmsManagementErrorCodes.CmsPublishValidationFailed);
            if (!refProj.CoverMediaAssetId.HasValue)
                return new ContentWorkflowResultDto(false, refProj.WorkflowStatus, refProj.PublishedAt, refProj.PublishAt, CmsManagementErrorCodes.ReferenceCoverRequired);
        }

        if (targetStatus == ContentWorkflowStatus.Scheduled && (!publishAt.HasValue || publishAt.Value <= DateTimeOffset.UtcNow))
            return new ContentWorkflowResultDto(false, refProj.WorkflowStatus, refProj.PublishedAt, refProj.PublishAt, CmsManagementErrorCodes.CmsScheduleDateInvalid);

        if (rowVersion.Length > 0 && refProj.RowVersion.Length > 0 && !refProj.RowVersion.SequenceEqual(rowVersion))
            throw new DbUpdateConcurrencyException(CmsManagementErrorCodes.CmsConcurrencyConflict);

        refProj.SetWorkflowStatus(targetStatus, publishAt, actorUserId);
        await dbContext.SaveChangesAsync(cancellationToken);

        await revisionService.CreateRevisionAsync("ReferenceProject", id, MapChangeType(targetStatus), actorUserId, reason, null, cancellationToken);
        return new ContentWorkflowResultDto(true, refProj.WorkflowStatus, refProj.PublishedAt, refProj.PublishAt, null);
    }

    private async Task<ContentWorkflowResultDto> TransitionShowroomAsync(Guid id, ContentWorkflowStatus targetStatus, DateTimeOffset? publishAt, string? reason, byte[] rowVersion, Guid actorUserId, CancellationToken cancellationToken)
    {
        var showroom = await dbContext.Set<Showroom>()
            .Include(s => s.Translations)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (showroom is null) return new ContentWorkflowResultDto(false, ContentWorkflowStatus.Draft, null, null, CmsManagementErrorCodes.ShowroomNotFound);

        if (!CanTransition(showroom.WorkflowStatus, targetStatus))
            return new ContentWorkflowResultDto(false, showroom.WorkflowStatus, showroom.PublishedAt, showroom.PublishAt, CmsManagementErrorCodes.CmsWorkflowTransitionInvalid);

        if (targetStatus == ContentWorkflowStatus.Published)
        {
            var tr = showroom.Translations.FirstOrDefault(t => t.LanguageCode == "tr");
            if (tr is null || string.IsNullOrWhiteSpace(tr.Title) || string.IsNullOrWhiteSpace(tr.Slug))
                return new ContentWorkflowResultDto(false, showroom.WorkflowStatus, showroom.PublishedAt, showroom.PublishAt, CmsManagementErrorCodes.CmsPublishValidationFailed);
        }

        if (targetStatus == ContentWorkflowStatus.Scheduled && (!publishAt.HasValue || publishAt.Value <= DateTimeOffset.UtcNow))
            return new ContentWorkflowResultDto(false, showroom.WorkflowStatus, showroom.PublishedAt, showroom.PublishAt, CmsManagementErrorCodes.CmsScheduleDateInvalid);

        if (rowVersion.Length > 0 && showroom.RowVersion.Length > 0 && !showroom.RowVersion.SequenceEqual(rowVersion))
            throw new DbUpdateConcurrencyException(CmsManagementErrorCodes.CmsConcurrencyConflict);

        showroom.SetWorkflowStatus(targetStatus, publishAt, actorUserId);
        await dbContext.SaveChangesAsync(cancellationToken);

        await revisionService.CreateRevisionAsync("Showroom", id, MapChangeType(targetStatus), actorUserId, reason, null, cancellationToken);
        return new ContentWorkflowResultDto(true, showroom.WorkflowStatus, showroom.PublishedAt, showroom.PublishAt, null);
    }

    private async Task<ContentWorkflowResultDto> TransitionBannerAsync(Guid id, ContentWorkflowStatus targetStatus, DateTimeOffset? publishAt, string? reason, byte[] rowVersion, Guid actorUserId, CancellationToken cancellationToken)
    {
        var banner = await dbContext.Set<Banner>().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (banner is null) return new ContentWorkflowResultDto(false, ContentWorkflowStatus.Draft, null, null, CmsManagementErrorCodes.BannerNotFound);

        if (!CanTransition(banner.WorkflowStatus, targetStatus))
            return new ContentWorkflowResultDto(false, banner.WorkflowStatus, banner.PublishedAt, banner.PublishAt, CmsManagementErrorCodes.CmsWorkflowTransitionInvalid);

        if (rowVersion.Length > 0 && banner.RowVersion.Length > 0 && !banner.RowVersion.SequenceEqual(rowVersion))
            throw new DbUpdateConcurrencyException(CmsManagementErrorCodes.CmsConcurrencyConflict);

        banner.SetWorkflowStatus(targetStatus, publishAt, actorUserId);
        await dbContext.SaveChangesAsync(cancellationToken);

        await revisionService.CreateRevisionAsync("Banner", id, MapChangeType(targetStatus), actorUserId, reason, null, cancellationToken);
        return new ContentWorkflowResultDto(true, banner.WorkflowStatus, banner.PublishedAt, banner.PublishAt, null);
    }

    private static CmsRevisionChangeType MapChangeType(ContentWorkflowStatus status) => status switch
    {
        ContentWorkflowStatus.Published => CmsRevisionChangeType.Published,
        ContentWorkflowStatus.Unpublished => CmsRevisionChangeType.Unpublished,
        ContentWorkflowStatus.Scheduled => CmsRevisionChangeType.Scheduled,
        ContentWorkflowStatus.Archived => CmsRevisionChangeType.Archived,
        _ => CmsRevisionChangeType.Updated
    };
}
