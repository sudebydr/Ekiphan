using Ekiphan.Application.Content;
using Ekiphan.Domain.Content;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Content;

public sealed class BannerManagementService(
    EkiphanDbContext dbContext,
    IUrlSafetyService urlSafetyService,
    ICmsRevisionService revisionService,
    ICmsCacheInvalidationService cacheService)
    : IBannerManagementService
{
    public async Task<IReadOnlyList<BannerGroupDto>> GetBannerGroupsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<BannerGroup>().AsNoTracking()
            .OrderBy(g => g.Placement)
            .Select(g => new BannerGroupDto(g.Id, g.Code, g.Name, g.Placement, g.IsActive, g.CreatedAt, g.RowVersion))
            .ToListAsync(cancellationToken);
    }

    public async Task<BannerGroupDto> CreateBannerGroupAsync(CreateBannerGroupCommand command, CancellationToken cancellationToken = default)
    {
        var group = new BannerGroup(Guid.NewGuid(), command.Code, command.Name, command.Placement, command.IsActive);
        dbContext.Set<BannerGroup>().Add(group);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new BannerGroupDto(group.Id, group.Code, group.Name, group.Placement, group.IsActive, group.CreatedAt, group.RowVersion);
    }

    public async Task ArchiveBannerGroupAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await dbContext.Set<BannerGroup>().FirstOrDefaultAsync(g => g.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.BannerGroupNotFound);

        group.Update(group.Name, group.Placement, false);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Banners: true), cancellationToken);
    }

    public async Task<IReadOnlyList<BannerListItemDto>> GetBannersAsync(Guid? groupId = null, BannerPlacement? placement = null, ContentWorkflowStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Banner>().AsNoTracking().Include(b => b.Translations).AsQueryable();

        if (groupId.HasValue) query = query.Where(b => b.BannerGroupId == groupId.Value);
        if (status.HasValue) query = query.Where(b => b.WorkflowStatus == status.Value);

        if (placement.HasValue)
        {
            var groupIds = await dbContext.Set<BannerGroup>().AsNoTracking().Where(g => g.Placement == placement.Value && g.IsActive).Select(g => g.Id).ToListAsync(cancellationToken);
            query = query.Where(b => groupIds.Contains(b.BannerGroupId));
        }

        var banners = await query.OrderBy(b => b.SortOrder).ThenByDescending(b => b.CreatedAt).ToListAsync(cancellationToken);

        return banners.Select(b => new BannerListItemDto(
            b.Id, b.BannerGroupId, placement ?? BannerPlacement.HomeHero, b.DesktopMediaAssetId,
            b.MobileMediaAssetId, b.LinkUrl, b.LinkTarget, b.WorkflowStatus, b.PublishAt,
            b.PublishEndAt, b.SortOrder, b.IsActive, b.CreatedAt, b.RowVersion)).ToList();
    }

    public async Task<BannerDetailDto?> GetBannerByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var b = await dbContext.Set<Banner>().AsNoTracking().Include(x => x.Translations).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (b is null) return null;

        var translations = b.Translations.Select(t => new BannerTranslationDto(t.LanguageCode, t.Title, t.Subtitle, t.Description, t.CtaText, t.AccessibleLabel)).ToList();
        return new BannerDetailDto(b.Id, b.BannerGroupId, b.DesktopMediaAssetId, b.MobileMediaAssetId, b.LinkUrl, b.LinkTarget, b.WorkflowStatus, b.PublishAt, b.PublishEndAt, b.SortOrder, b.IsActive, b.CreatedAt, b.RowVersion, translations);
    }

    public async Task<BannerDetailDto> CreateBannerAsync(CreateBannerCommand command, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(command.LinkUrl) && !urlSafetyService.IsSafeUrl(command.LinkUrl))
            throw new ArgumentException(CmsManagementErrorCodes.BannerUrlInvalid, nameof(command));

        var groupExists = await dbContext.Set<BannerGroup>().AsNoTracking().AnyAsync(g => g.Id == command.BannerGroupId, cancellationToken);
        if (!groupExists) throw new KeyNotFoundException(CmsManagementErrorCodes.BannerGroupNotFound);

        var banner = new Banner(Guid.NewGuid(), command.BannerGroupId, command.DesktopMediaAssetId, command.MobileMediaAssetId, command.LinkUrl, command.LinkTarget, command.PublishAt, command.PublishEndAt, command.SortOrder, command.IsActive, command.ActorUserId);

        foreach (var tr in command.Translations)
        {
            banner.SetTranslation(tr.LanguageCode, tr.Title, tr.Subtitle, tr.Description, tr.CtaText, tr.AccessibleLabel);
        }

        dbContext.Set<Banner>().Add(banner);
        await dbContext.SaveChangesAsync(cancellationToken);

        await revisionService.CreateRevisionAsync("Banner", banner.Id, CmsRevisionChangeType.Created, command.ActorUserId, "Created banner.", null, cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Banners: true), cancellationToken);

        return (await GetBannerByIdAsync(banner.Id, cancellationToken))!;
    }

    public async Task<BannerDetailDto> UpdateBannerAsync(UpdateBannerCommand command, CancellationToken cancellationToken = default)
    {
        var banner = await dbContext.Set<Banner>().Include(b => b.Translations).FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.BannerNotFound);

        if (command.RowVersion.Length > 0 && banner.RowVersion.Length > 0 && !banner.RowVersion.SequenceEqual(command.RowVersion))
            throw new DbUpdateConcurrencyException(CmsManagementErrorCodes.CmsConcurrencyConflict);

        if (!string.IsNullOrWhiteSpace(command.LinkUrl) && !urlSafetyService.IsSafeUrl(command.LinkUrl))
            throw new ArgumentException(CmsManagementErrorCodes.BannerUrlInvalid, nameof(command));

        banner.Update(command.DesktopMediaAssetId, command.MobileMediaAssetId, command.LinkUrl, command.LinkTarget, command.PublishAt, command.PublishEndAt, command.IsActive, command.ActorUserId);

        foreach (var tr in command.Translations)
        {
            banner.SetTranslation(tr.LanguageCode, tr.Title, tr.Subtitle, tr.Description, tr.CtaText, tr.AccessibleLabel);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await revisionService.CreateRevisionAsync("Banner", banner.Id, CmsRevisionChangeType.Updated, command.ActorUserId, "Updated banner.", null, cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Banners: true), cancellationToken);

        return (await GetBannerByIdAsync(banner.Id, cancellationToken))!;
    }

    public async Task ReorderBannersAsync(Guid groupId, ReorderCmsItemsCommand command, CancellationToken cancellationToken = default)
    {
        var banners = await dbContext.Set<Banner>().Where(b => b.BannerGroupId == groupId).ToListAsync(cancellationToken);

        foreach (var item in command.Items)
        {
            var b = banners.FirstOrDefault(x => x.Id == item.Id);
            b?.SetSortOrder(item.SortOrder);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Banners: true), cancellationToken);
    }
}
