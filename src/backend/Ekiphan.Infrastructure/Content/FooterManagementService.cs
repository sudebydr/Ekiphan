using Ekiphan.Application.Content;
using Ekiphan.Domain.Content;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Content;

public sealed class FooterManagementService(
    EkiphanDbContext dbContext,
    IUrlSafetyService urlSafetyService,
    ICmsCacheInvalidationService cacheService)
    : IFooterManagementService
{
    public async Task<IReadOnlyList<FooterColumnDto>> GetFooterColumnsAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<FooterColumn>().AsNoTracking().Include(c => c.Links).AsQueryable();
        if (activeOnly) query = query.Where(c => c.IsActive);

        var cols = await query.OrderBy(c => c.SortOrder).ToListAsync(cancellationToken);

        return cols.Select(c => new FooterColumnDto(
            c.Id, c.Code, c.TitleTr, c.TitleEn, c.SortOrder, c.IsActive, c.RowVersion,
            c.Links.Where(l => !activeOnly || l.IsActive).OrderBy(l => l.SortOrder)
                .Select(l => new FooterLinkDto(l.Id, l.FooterColumnId, l.LabelTr, l.LabelEn, l.Url, l.LinkTarget, l.SortOrder, l.IsActive, l.RowVersion)).ToList()))
            .ToList();
    }

    public async Task<FooterColumnDto> CreateColumnAsync(CreateFooterColumnCommand command, CancellationToken cancellationToken = default)
    {
        var col = new FooterColumn(Guid.NewGuid(), command.Code, command.TitleTr, command.TitleEn, command.SortOrder, command.IsActive);
        dbContext.Set<FooterColumn>().Add(col);
        await dbContext.SaveChangesAsync(cancellationToken);

        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Footer: true), cancellationToken);
        return new FooterColumnDto(col.Id, col.Code, col.TitleTr, col.TitleEn, col.SortOrder, col.IsActive, col.RowVersion, []);
    }

    public async Task<FooterColumnDto> UpdateColumnAsync(UpdateFooterColumnCommand command, CancellationToken cancellationToken = default)
    {
        var col = await dbContext.Set<FooterColumn>().Include(c => c.Links).FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.FooterColumnNotFound);

        if (command.RowVersion.Length > 0 && col.RowVersion.Length > 0 && !col.RowVersion.SequenceEqual(command.RowVersion))
            throw new DbUpdateConcurrencyException(CmsManagementErrorCodes.CmsConcurrencyConflict);

        col.Update(command.TitleTr, command.TitleEn, command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);

        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Footer: true), cancellationToken);
        return new FooterColumnDto(
            col.Id, col.Code, col.TitleTr, col.TitleEn, col.SortOrder, col.IsActive, col.RowVersion,
            col.Links.Select(l => new FooterLinkDto(l.Id, l.FooterColumnId, l.LabelTr, l.LabelEn, l.Url, l.LinkTarget, l.SortOrder, l.IsActive, l.RowVersion)).ToList());
    }

    public async Task DeleteColumnAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var col = await dbContext.Set<FooterColumn>().FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.FooterColumnNotFound);

        dbContext.Set<FooterColumn>().Remove(col);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Footer: true), cancellationToken);
    }

    public async Task ReorderColumnsAsync(ReorderCmsItemsCommand command, CancellationToken cancellationToken = default)
    {
        var ids = command.Items.Select(i => i.Id).ToList();
        var cols = await dbContext.Set<FooterColumn>().Where(c => ids.Contains(c.Id)).ToListAsync(cancellationToken);

        foreach (var item in command.Items)
        {
            var col = cols.FirstOrDefault(c => c.Id == item.Id);
            col?.SetSortOrder(item.SortOrder);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Footer: true), cancellationToken);
    }

    public async Task<FooterLinkDto> CreateLinkAsync(CreateFooterLinkCommand command, CancellationToken cancellationToken = default)
    {
        if (!urlSafetyService.IsSafeUrl(command.Url))
            throw new ArgumentException(CmsManagementErrorCodes.FooterUrlInvalid, nameof(command));

        var col = await dbContext.Set<FooterColumn>().Include(c => c.Links).FirstOrDefaultAsync(c => c.Id == command.FooterColumnId, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.FooterColumnNotFound);

        var link = col.AddLink(command.LabelTr, command.LabelEn, command.Url, command.LinkTarget, command.SortOrder, command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);

        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Footer: true), cancellationToken);
        return new FooterLinkDto(link.Id, link.FooterColumnId, link.LabelTr, link.LabelEn, link.Url, link.LinkTarget, link.SortOrder, link.IsActive, link.RowVersion);
    }

    public async Task<FooterLinkDto> UpdateLinkAsync(UpdateFooterLinkCommand command, CancellationToken cancellationToken = default)
    {
        if (!urlSafetyService.IsSafeUrl(command.Url))
            throw new ArgumentException(CmsManagementErrorCodes.FooterUrlInvalid, nameof(command));

        var link = await dbContext.Set<FooterLink>().FirstOrDefaultAsync(l => l.Id == command.Id, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.FooterLinkNotFound);

        if (command.RowVersion.Length > 0 && link.RowVersion.Length > 0 && !link.RowVersion.SequenceEqual(command.RowVersion))
            throw new DbUpdateConcurrencyException(CmsManagementErrorCodes.CmsConcurrencyConflict);

        link.Update(command.LabelTr, command.LabelEn, command.Url, command.LinkTarget, command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);

        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Footer: true), cancellationToken);
        return new FooterLinkDto(link.Id, link.FooterColumnId, link.LabelTr, link.LabelEn, link.Url, link.LinkTarget, link.SortOrder, link.IsActive, link.RowVersion);
    }

    public async Task DeleteLinkAsync(Guid linkId, CancellationToken cancellationToken = default)
    {
        var link = await dbContext.Set<FooterLink>().FirstOrDefaultAsync(l => l.Id == linkId, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.FooterLinkNotFound);

        dbContext.Set<FooterLink>().Remove(link);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Footer: true), cancellationToken);
    }

    public async Task ReorderLinksAsync(Guid columnId, ReorderCmsItemsCommand command, CancellationToken cancellationToken = default)
    {
        var col = await dbContext.Set<FooterColumn>().Include(c => c.Links).FirstOrDefaultAsync(c => c.Id == columnId, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.FooterColumnNotFound);

        foreach (var item in command.Items)
        {
            var link = col.Links.FirstOrDefault(l => l.Id == item.Id);
            link?.SetSortOrder(item.SortOrder);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Footer: true), cancellationToken);
    }
}
