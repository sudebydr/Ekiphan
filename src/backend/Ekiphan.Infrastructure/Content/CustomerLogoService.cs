using Ekiphan.Application.Content;
using Ekiphan.Domain.Content;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Content;

public sealed class CustomerLogoService(
    EkiphanDbContext dbContext,
    IUrlSafetyService urlSafetyService,
    ICmsCacheInvalidationService cacheService)
    : ICustomerLogoService
{
    public async Task<IReadOnlyList<CustomerLogoDto>> GetLogosAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<CustomerLogo>().AsNoTracking();
        if (activeOnly) query = query.Where(l => l.IsActive && l.ArchivedAt == null);

        return await query.OrderBy(l => l.SortOrder).ThenByDescending(l => l.CreatedAt)
            .Select(l => new CustomerLogoDto(
                l.Id, l.Name, l.WebsiteUrl, l.MediaAssetId, l.AltTextTr, l.AltTextEn,
                l.SortOrder, l.IsActive, l.IsFeatured, l.CreatedAt, l.RowVersion))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerLogoDto?> GetLogoByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var l = await dbContext.Set<CustomerLogo>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (l is null) return null;
        return new CustomerLogoDto(l.Id, l.Name, l.WebsiteUrl, l.MediaAssetId, l.AltTextTr, l.AltTextEn, l.SortOrder, l.IsActive, l.IsFeatured, l.CreatedAt, l.RowVersion);
    }

    public async Task<CustomerLogoDto> CreateLogoAsync(CreateCustomerLogoCommand command, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(command.WebsiteUrl) && !urlSafetyService.IsSafeUrl(command.WebsiteUrl))
            throw new ArgumentException(CmsManagementErrorCodes.FooterUrlInvalid, nameof(command));

        var duplicate = await dbContext.Set<CustomerLogo>().AsNoTracking().AnyAsync(l => l.Name == command.Name.Trim() && l.MediaAssetId == command.MediaAssetId, cancellationToken);
        if (duplicate) throw new InvalidOperationException(CmsManagementErrorCodes.CustomerLogoDuplicate);

        var logo = new CustomerLogo(Guid.NewGuid(), command.Name, command.MediaAssetId, command.WebsiteUrl, command.AltTextTr, command.AltTextEn, command.SortOrder, command.IsActive, command.IsFeatured, command.ActorUserId);
        dbContext.Set<CustomerLogo>().Add(logo);
        await dbContext.SaveChangesAsync(cancellationToken);

        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(CustomerLogos: true), cancellationToken);
        return (await GetLogoByIdAsync(logo.Id, cancellationToken))!;
    }

    public async Task<CustomerLogoDto> UpdateLogoAsync(UpdateCustomerLogoCommand command, CancellationToken cancellationToken = default)
    {
        var logo = await dbContext.Set<CustomerLogo>().FirstOrDefaultAsync(l => l.Id == command.Id, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.CustomerLogoNotFound);

        if (command.RowVersion.Length > 0 && logo.RowVersion.Length > 0 && !logo.RowVersion.SequenceEqual(command.RowVersion))
            throw new DbUpdateConcurrencyException(CmsManagementErrorCodes.CmsConcurrencyConflict);

        if (!string.IsNullOrWhiteSpace(command.WebsiteUrl) && !urlSafetyService.IsSafeUrl(command.WebsiteUrl))
            throw new ArgumentException(CmsManagementErrorCodes.FooterUrlInvalid, nameof(command));

        logo.Update(command.Name, command.MediaAssetId, command.WebsiteUrl, command.AltTextTr, command.AltTextEn, command.IsActive, command.IsFeatured, command.ActorUserId);
        await dbContext.SaveChangesAsync(cancellationToken);

        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(CustomerLogos: true), cancellationToken);
        return (await GetLogoByIdAsync(logo.Id, cancellationToken))!;
    }

    public async Task ArchiveLogoAsync(Guid id, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var logo = await dbContext.Set<CustomerLogo>().FirstOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.CustomerLogoNotFound);

        logo.Archive(actorUserId);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(CustomerLogos: true), cancellationToken);
    }

    public async Task ReorderLogosAsync(ReorderCmsItemsCommand command, CancellationToken cancellationToken = default)
    {
        var ids = command.Items.Select(i => i.Id).ToList();
        var logos = await dbContext.Set<CustomerLogo>().Where(l => ids.Contains(l.Id)).ToListAsync(cancellationToken);

        foreach (var item in command.Items)
        {
            var logo = logos.FirstOrDefault(l => l.Id == item.Id);
            logo?.SetSortOrder(item.SortOrder);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(CustomerLogos: true), cancellationToken);
    }
}
