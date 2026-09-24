using Ekiphan.Application.Content;
using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.Content;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Content;

public sealed class ReferenceProjectService(
    EkiphanDbContext dbContext,
    ICmsRevisionService revisionService,
    ICmsCacheInvalidationService cacheService)
    : IReferenceProjectService
{
    public async Task<IReadOnlyList<ReferenceProjectListItemDto>> GetReferencesAsync(ContentWorkflowStatus? status = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<ReferenceProject>().AsNoTracking();

        if (status.HasValue)
            query = query.Where(r => r.WorkflowStatus == status.Value);
        else
            query = query.Where(r => r.WorkflowStatus != ContentWorkflowStatus.Archived);

        var p = page <= 0 ? 1 : page;
        var ps = pageSize <= 0 ? 20 : pageSize;

        return await query.OrderBy(r => r.SortOrder).ThenByDescending(r => r.CreatedAt)
            .Skip((p - 1) * ps).Take(ps)
            .Select(r => new ReferenceProjectListItemDto(
                r.Id, r.CustomerName, r.ProjectDate, r.Location, r.CoverMediaAssetId,
                r.WorkflowStatus, r.PublishAt, r.PublishedAt, r.SortOrder, r.IsFeatured,
                r.CreatedAt, r.RowVersion))
            .ToListAsync(cancellationToken);
    }

    public async Task<ReferenceProjectDetailDto?> GetReferenceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var r = await dbContext.Set<ReferenceProject>()
            .AsNoTracking()
            .Include(x => x.Translations)
            .Include(x => x.Media)
            .Include(x => x.Products)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (r is null) return null;
        return await MapToDetailDtoAsync(r, cancellationToken);
    }

    public async Task<ReferenceProjectDetailDto?> GetReferenceBySlugAsync(string slug, string languageCode, CancellationToken cancellationToken = default)
    {
        var normSlug = slug.Trim().ToLowerInvariant();
        var normLang = languageCode.Trim().ToLowerInvariant();

        var r = await dbContext.Set<ReferenceProject>()
            .AsNoTracking()
            .Include(x => x.Translations)
            .Include(x => x.Media)
            .Include(x => x.Products)
            .FirstOrDefaultAsync(x => x.WorkflowStatus == ContentWorkflowStatus.Published &&
                                     x.Translations.Any(t => t.LanguageCode == normLang && t.Slug == normSlug), cancellationToken);

        if (r is null) return null;
        return await MapToDetailDtoAsync(r, cancellationToken);
    }

    public async Task<ReferenceProjectDetailDto> CreateReferenceAsync(CreateReferenceProjectCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateSlugsAsync(null, command.Translations, cancellationToken);

        var r = new ReferenceProject(Guid.NewGuid(), command.CustomerName, command.ProjectDate, command.Location, command.CoverMediaAssetId, 0, command.IsFeatured, command.ActorUserId);

        foreach (var tr in command.Translations)
        {
            r.SetTranslation(tr.LanguageCode, tr.Title, tr.Slug, tr.ShortDescription, tr.LongDescription, tr.MetaTitle, tr.MetaDescription, tr.CanonicalUrl, tr.OpenGraphTitle, tr.OpenGraphDescription, tr.OpenGraphMediaAssetId);
        }

        dbContext.Set<ReferenceProject>().Add(r);
        await dbContext.SaveChangesAsync(cancellationToken);

        await revisionService.CreateRevisionAsync("ReferenceProject", r.Id, CmsRevisionChangeType.Created, command.ActorUserId, "Created reference project.", null, cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(References: true), cancellationToken);

        return (await GetReferenceByIdAsync(r.Id, cancellationToken))!;
    }

    public async Task<ReferenceProjectDetailDto> UpdateReferenceAsync(UpdateReferenceProjectCommand command, CancellationToken cancellationToken = default)
    {
        var r = await dbContext.Set<ReferenceProject>()
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.ReferenceNotFound);

        if (command.RowVersion.Length > 0 && r.RowVersion.Length > 0 && !r.RowVersion.SequenceEqual(command.RowVersion))
            throw new DbUpdateConcurrencyException(CmsManagementErrorCodes.CmsConcurrencyConflict);

        await ValidateSlugsAsync(r.Id, command.Translations, cancellationToken);

        r.UpdateIdentity(command.CustomerName, command.ProjectDate, command.Location, command.CoverMediaAssetId, command.IsFeatured, command.ActorUserId);

        foreach (var tr in command.Translations)
        {
            r.SetTranslation(tr.LanguageCode, tr.Title, tr.Slug, tr.ShortDescription, tr.LongDescription, tr.MetaTitle, tr.MetaDescription, tr.CanonicalUrl, tr.OpenGraphTitle, tr.OpenGraphDescription, tr.OpenGraphMediaAssetId);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await revisionService.CreateRevisionAsync("ReferenceProject", r.Id, CmsRevisionChangeType.Updated, command.ActorUserId, "Updated reference project.", null, cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(References: true), cancellationToken);

        return (await GetReferenceByIdAsync(r.Id, cancellationToken))!;
    }

    public async Task AddMediaAsync(AddReferenceProjectMediaCommand command, CancellationToken cancellationToken = default)
    {
        var r = await dbContext.Set<ReferenceProject>()
            .Include(x => x.Media)
            .FirstOrDefaultAsync(x => x.Id == command.ReferenceProjectId, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.ReferenceNotFound);

        r.AddMedia(command.MediaAssetId, command.SortOrder, command.IsCover, command.CaptionTr, command.CaptionEn);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(References: true), cancellationToken);
    }

    public async Task RemoveMediaAsync(Guid referenceId, Guid mediaAssetId, CancellationToken cancellationToken = default)
    {
        var r = await dbContext.Set<ReferenceProject>()
            .Include(x => x.Media)
            .FirstOrDefaultAsync(x => x.Id == referenceId, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.ReferenceNotFound);

        r.RemoveMedia(mediaAssetId);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(References: true), cancellationToken);
    }

    public async Task ReorderMediaAsync(Guid referenceId, ReorderCmsItemsCommand command, CancellationToken cancellationToken = default)
    {
        var r = await dbContext.Set<ReferenceProject>()
            .Include(x => x.Media)
            .FirstOrDefaultAsync(x => x.Id == referenceId, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.ReferenceNotFound);

        foreach (var item in command.Items)
        {
            var media = r.Media.FirstOrDefault(m => m.MediaAssetId == item.Id || m.Id == item.Id);
            media?.SetSortOrder(item.SortOrder);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(References: true), cancellationToken);
    }

    public async Task AddProductAsync(AddReferenceProjectProductCommand command, CancellationToken cancellationToken = default)
    {
        var r = await dbContext.Set<ReferenceProject>()
            .Include(x => x.Products)
            .FirstOrDefaultAsync(x => x.Id == command.ReferenceProjectId, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.ReferenceNotFound);

        var productExists = await dbContext.Products.AsNoTracking().AnyAsync(p => p.Id == command.ProductId && !p.IsDeleted, cancellationToken);
        if (!productExists) throw new KeyNotFoundException("Product not found or soft deleted.");

        r.AddProduct(command.ProductId, command.SortOrder, command.Description);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(References: true), cancellationToken);
    }

    public async Task RemoveProductAsync(Guid referenceId, Guid productId, CancellationToken cancellationToken = default)
    {
        var r = await dbContext.Set<ReferenceProject>()
            .Include(x => x.Products)
            .FirstOrDefaultAsync(x => x.Id == referenceId, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.ReferenceNotFound);

        r.RemoveProduct(productId);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(References: true), cancellationToken);
    }

    public async Task ReorderProductsAsync(Guid referenceId, ReorderCmsItemsCommand command, CancellationToken cancellationToken = default)
    {
        var r = await dbContext.Set<ReferenceProject>()
            .Include(x => x.Products)
            .FirstOrDefaultAsync(x => x.Id == referenceId, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.ReferenceNotFound);

        foreach (var item in command.Items)
        {
            var prod = r.Products.FirstOrDefault(p => p.ProductId == item.Id);
            prod?.SetSortOrder(item.SortOrder);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(References: true), cancellationToken);
    }

    private async Task ValidateSlugsAsync(Guid? currentId, IEnumerable<ReferenceProjectTranslationDto> translations, CancellationToken cancellationToken)
    {
        foreach (var tr in translations)
        {
            var normSlug = tr.Slug.Trim().ToLowerInvariant();
            var normLang = tr.LanguageCode.Trim().ToLowerInvariant();

            var duplicate = await dbContext.Set<ReferenceProjectTranslation>()
                .AsNoTracking()
                .AnyAsync(t => t.LanguageCode == normLang && t.Slug == normSlug && (!currentId.HasValue || t.ReferenceProjectId != currentId.Value), cancellationToken);

            if (duplicate) throw new InvalidOperationException(CmsManagementErrorCodes.CmsSlugDuplicate);
        }
    }

    private async Task<ReferenceProjectDetailDto> MapToDetailDtoAsync(ReferenceProject r, CancellationToken cancellationToken)
    {
        var prodIds = r.Products.Select(p => p.ProductId).ToList();
        var productsInfo = await dbContext.Products.AsNoTracking()
            .Where(p => prodIds.Contains(p.Id))
            .Select(p => new { p.Id, p.NormalizedSku, Name = p.Translations.Where(t => t.LanguageCode == "tr").Select(t => t.Name).FirstOrDefault() })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var productDtos = r.Products.Select(p => new ReferenceProjectProductDto(
            p.ProductId,
            productsInfo.TryGetValue(p.ProductId, out var pi) ? pi.NormalizedSku : string.Empty,
            pi?.Name,
            p.SortOrder,
            p.Description)).ToList();

        var translationDtos = r.Translations.Select(t => new ReferenceProjectTranslationDto(
            t.LanguageCode, t.Title, t.Slug, t.ShortDescription, t.LongDescription,
            t.MetaTitle, t.MetaDescription, t.CanonicalUrl, t.OpenGraphTitle,
            t.OpenGraphDescription, t.OpenGraphMediaAssetId)).ToList();

        var mediaDtos = r.Media.Select(m => new ReferenceProjectMediaDto(
            m.Id, m.MediaAssetId, m.SortOrder, m.IsCover, m.CaptionTr, m.CaptionEn, m.CreatedAt)).ToList();

        return new ReferenceProjectDetailDto(
            r.Id, r.CustomerName, r.ProjectDate, r.Location, r.CoverMediaAssetId,
            r.WorkflowStatus, r.PublishAt, r.PublishedAt, r.PublishedByUserId,
            r.SortOrder, r.IsFeatured, r.CreatedAt, r.CreatedByUserId,
            r.UpdatedAt, r.UpdatedByUserId, r.RowVersion,
            translationDtos, mediaDtos, productDtos);
    }
}
