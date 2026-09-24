using Ekiphan.Application.Content;
using Ekiphan.Domain.Content;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Ekiphan.Infrastructure.Content;

public sealed class ShowroomEmbedSanitizer(IConfiguration configuration) : IEmbedSanitizationService, IAllowedEmbedProviderService
{
    private readonly HashSet<string> _allowedHosts = new(
        configuration.GetSection("ShowroomEmbeds:AllowedHosts").Get<string[]>() ?? ["my.matterport.com", "kuula.co"],
        StringComparer.OrdinalIgnoreCase);

    public bool IsAllowed(Uri uri) => _allowedHosts.Contains(uri.Host);

    public EmbedSanitizationResult Sanitize(string? embedCode)
    {
        if (string.IsNullOrWhiteSpace(embedCode)) return new EmbedSanitizationResult(true, null, null);

        var trimmed = embedCode.Trim();

        if (trimmed.Contains("<script", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("javascript:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("onerror=", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("onload=", StringComparison.OrdinalIgnoreCase))
        {
            return new EmbedSanitizationResult(false, null, CmsManagementErrorCodes.ShowroomEmbedInvalid);
        }

        // Extract src URL from iframe if embed code is an <iframe>
        if (trimmed.StartsWith("<iframe", StringComparison.OrdinalIgnoreCase))
        {
            var srcIndex = trimmed.IndexOf("src=", StringComparison.OrdinalIgnoreCase);
            if (srcIndex >= 0)
            {
                var quoteChar = trimmed[srcIndex + 4];
                if (quoteChar is '"' or '\'')
                {
                    var endQuote = trimmed.IndexOf(quoteChar, srcIndex + 5);
                    if (endQuote > srcIndex + 5)
                    {
                        var urlStr = trimmed.Substring(srcIndex + 5, endQuote - (srcIndex + 5));
                        if (Uri.TryCreate(urlStr, UriKind.Absolute, out var uri) && !IsAllowed(uri))
                        {
                            return new EmbedSanitizationResult(false, null, CmsManagementErrorCodes.ShowroomEmbedProviderNotAllowed);
                        }
                    }
                }
            }
        }
        else if (Uri.TryCreate(trimmed, UriKind.Absolute, out var directUri))
        {
            if (!IsAllowed(directUri))
                return new EmbedSanitizationResult(false, null, CmsManagementErrorCodes.ShowroomEmbedProviderNotAllowed);
        }

        return new EmbedSanitizationResult(true, trimmed, null);
    }
}

public sealed class ShowroomService(
    EkiphanDbContext dbContext,
    IEmbedSanitizationService embedSanitizer,
    ICmsRevisionService revisionService,
    ICmsCacheInvalidationService cacheService)
    : IShowroomService
{
    public async Task<IReadOnlyList<ShowroomListItemDto>> GetShowroomsAsync(ContentWorkflowStatus? status = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Showroom>().AsNoTracking();
        if (status.HasValue)
            query = query.Where(s => s.WorkflowStatus == status.Value);
        else
            query = query.Where(s => s.WorkflowStatus != ContentWorkflowStatus.Archived);

        var p = page <= 0 ? 1 : page;
        var ps = pageSize <= 0 ? 20 : pageSize;

        return await query.OrderBy(s => s.SortOrder).ThenByDescending(s => s.CreatedAt)
            .Skip((p - 1) * ps).Take(ps)
            .Select(s => new ShowroomListItemDto(
                s.Id, s.CoverMediaAssetId, s.VirtualTourUrl, s.WorkflowStatus,
                s.PublishAt, s.PublishedAt, s.SortOrder, s.IsFeatured, s.CreatedAt, s.RowVersion))
            .ToListAsync(cancellationToken);
    }

    public async Task<ShowroomDetailDto?> GetShowroomByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var s = await dbContext.Set<Showroom>()
            .AsNoTracking()
            .Include(x => x.Translations)
            .Include(x => x.Media)
            .Include(x => x.Hotspots)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (s is null) return null;
        return await MapToDetailDtoAsync(s, cancellationToken);
    }

    public async Task<ShowroomDetailDto?> GetShowroomBySlugAsync(string slug, string languageCode, CancellationToken cancellationToken = default)
    {
        var normSlug = slug.Trim().ToLowerInvariant();
        var normLang = languageCode.Trim().ToLowerInvariant();

        var s = await dbContext.Set<Showroom>()
            .AsNoTracking()
            .Include(x => x.Translations)
            .Include(x => x.Media)
            .Include(x => x.Hotspots)
            .FirstOrDefaultAsync(x => x.WorkflowStatus == ContentWorkflowStatus.Published &&
                                     x.Translations.Any(t => t.LanguageCode == normLang && t.Slug == normSlug), cancellationToken);

        if (s is null) return null;
        return await MapToDetailDtoAsync(s, cancellationToken);
    }

    public async Task<ShowroomDetailDto> CreateShowroomAsync(CreateShowroomCommand command, CancellationToken cancellationToken = default)
    {
        var sanitization = embedSanitizer.Sanitize(command.EmbedCode);
        if (!sanitization.IsAllowed)
            throw new InvalidOperationException(sanitization.ErrorMessage ?? CmsManagementErrorCodes.ShowroomEmbedInvalid);

        var s = new Showroom(Guid.NewGuid(), command.CoverMediaAssetId, command.VirtualTourUrl, sanitization.SanitizedHtml, 0, command.IsFeatured, command.ActorUserId);

        foreach (var tr in command.Translations)
        {
            s.SetTranslation(tr.LanguageCode, tr.Title, tr.Slug, tr.ShortDescription, tr.LongDescription, tr.MetaTitle, tr.MetaDescription, tr.OpenGraphTitle, tr.OpenGraphDescription, tr.OpenGraphMediaAssetId);
        }

        dbContext.Set<Showroom>().Add(s);
        await dbContext.SaveChangesAsync(cancellationToken);

        await revisionService.CreateRevisionAsync("Showroom", s.Id, CmsRevisionChangeType.Created, command.ActorUserId, "Created showroom.", null, cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Showrooms: true), cancellationToken);

        return (await GetShowroomByIdAsync(s.Id, cancellationToken))!;
    }

    public async Task<ShowroomDetailDto> UpdateShowroomAsync(UpdateShowroomCommand command, CancellationToken cancellationToken = default)
    {
        var s = await dbContext.Set<Showroom>()
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.ShowroomNotFound);

        if (command.RowVersion.Length > 0 && s.RowVersion.Length > 0 && !s.RowVersion.SequenceEqual(command.RowVersion))
            throw new DbUpdateConcurrencyException(CmsManagementErrorCodes.CmsConcurrencyConflict);

        var sanitization = embedSanitizer.Sanitize(command.EmbedCode);
        if (!sanitization.IsAllowed)
            throw new InvalidOperationException(sanitization.ErrorMessage ?? CmsManagementErrorCodes.ShowroomEmbedInvalid);

        s.Update(command.CoverMediaAssetId, command.VirtualTourUrl, sanitization.SanitizedHtml, command.IsFeatured, command.ActorUserId);

        foreach (var tr in command.Translations)
        {
            s.SetTranslation(tr.LanguageCode, tr.Title, tr.Slug, tr.ShortDescription, tr.LongDescription, tr.MetaTitle, tr.MetaDescription, tr.OpenGraphTitle, tr.OpenGraphDescription, tr.OpenGraphMediaAssetId);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await revisionService.CreateRevisionAsync("Showroom", s.Id, CmsRevisionChangeType.Updated, command.ActorUserId, "Updated showroom.", null, cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Showrooms: true), cancellationToken);

        return (await GetShowroomByIdAsync(s.Id, cancellationToken))!;
    }

    public async Task AddMediaAsync(Guid showroomId, Guid mediaAssetId, int sortOrder, string? captionTr, string? captionEn, CancellationToken cancellationToken = default)
    {
        var s = await dbContext.Set<Showroom>().Include(x => x.Media).FirstOrDefaultAsync(x => x.Id == showroomId, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.ShowroomNotFound);

        s.AddMedia(mediaAssetId, sortOrder, captionTr, captionEn);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Showrooms: true), cancellationToken);
    }

    public async Task RemoveMediaAsync(Guid showroomId, Guid mediaAssetId, CancellationToken cancellationToken = default)
    {
        var s = await dbContext.Set<Showroom>().Include(x => x.Media).FirstOrDefaultAsync(x => x.Id == showroomId, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.ShowroomNotFound);

        s.RemoveMedia(mediaAssetId);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Showrooms: true), cancellationToken);
    }

    public async Task ReorderMediaAsync(Guid showroomId, ReorderCmsItemsCommand command, CancellationToken cancellationToken = default)
    {
        var s = await dbContext.Set<Showroom>().Include(x => x.Media).FirstOrDefaultAsync(x => x.Id == showroomId, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.ShowroomNotFound);

        foreach (var item in command.Items)
        {
            var media = s.Media.FirstOrDefault(m => m.MediaAssetId == item.Id || m.Id == item.Id);
            media?.SetSortOrder(item.SortOrder);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Showrooms: true), cancellationToken);
    }

    public async Task<ShowroomHotspotDto> CreateHotspotAsync(CreateShowroomHotspotCommand command, CancellationToken cancellationToken = default)
    {
        var s = await dbContext.Set<Showroom>().Include(x => x.Hotspots).FirstOrDefaultAsync(x => x.Id == command.ShowroomId, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.ShowroomNotFound);

        if (command.ProductId.HasValue)
        {
            var pExists = await dbContext.Products.AsNoTracking().AnyAsync(p => p.Id == command.ProductId.Value && !p.IsDeleted, cancellationToken);
            if (!pExists) throw new KeyNotFoundException(CmsManagementErrorCodes.ShowroomProductNotFound);
        }

        var hotspot = s.AddHotspot(command.ProductId, command.TitleTr, command.TitleEn, command.DescriptionTr, command.DescriptionEn, command.PositionX, command.PositionY, command.PositionZ, command.SceneIdentifier, command.SortOrder, command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Showrooms: true), cancellationToken);

        return new ShowroomHotspotDto(
            hotspot.Id, hotspot.ShowroomId, hotspot.ProductId, null, null, hotspot.TitleTr, hotspot.TitleEn,
            hotspot.DescriptionTr, hotspot.DescriptionEn, hotspot.PositionX, hotspot.PositionY, hotspot.PositionZ,
            hotspot.SceneIdentifier, hotspot.SortOrder, hotspot.IsActive, hotspot.RowVersion);
    }

    public async Task<ShowroomHotspotDto> UpdateHotspotAsync(UpdateShowroomHotspotCommand command, CancellationToken cancellationToken = default)
    {
        var s = await dbContext.Set<Showroom>().Include(x => x.Hotspots).FirstOrDefaultAsync(x => x.Id == command.ShowroomId, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.ShowroomNotFound);

        var hotspot = s.Hotspots.FirstOrDefault(h => h.Id == command.HotspotId)
            ?? throw new KeyNotFoundException("Hotspot not found.");

        if (command.RowVersion.Length > 0 && hotspot.RowVersion.Length > 0 && !hotspot.RowVersion.SequenceEqual(command.RowVersion))
            throw new DbUpdateConcurrencyException(CmsManagementErrorCodes.CmsConcurrencyConflict);

        hotspot.Update(command.ProductId, command.TitleTr, command.TitleEn, command.DescriptionTr, command.DescriptionEn, command.PositionX, command.PositionY, command.PositionZ, command.SceneIdentifier, command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Showrooms: true), cancellationToken);

        return new ShowroomHotspotDto(
            hotspot.Id, hotspot.ShowroomId, hotspot.ProductId, null, null, hotspot.TitleTr, hotspot.TitleEn,
            hotspot.DescriptionTr, hotspot.DescriptionEn, hotspot.PositionX, hotspot.PositionY, hotspot.PositionZ,
            hotspot.SceneIdentifier, hotspot.SortOrder, hotspot.IsActive, hotspot.RowVersion);
    }

    public async Task DeleteHotspotAsync(Guid showroomId, Guid hotspotId, CancellationToken cancellationToken = default)
    {
        var s = await dbContext.Set<Showroom>().Include(x => x.Hotspots).FirstOrDefaultAsync(x => x.Id == showroomId, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.ShowroomNotFound);

        s.RemoveHotspot(hotspotId);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Showrooms: true), cancellationToken);
    }

    public async Task ReorderHotspotsAsync(Guid showroomId, ReorderCmsItemsCommand command, CancellationToken cancellationToken = default)
    {
        var s = await dbContext.Set<Showroom>().Include(x => x.Hotspots).FirstOrDefaultAsync(x => x.Id == showroomId, cancellationToken)
            ?? throw new KeyNotFoundException(CmsManagementErrorCodes.ShowroomNotFound);

        foreach (var item in command.Items)
        {
            var hotspot = s.Hotspots.FirstOrDefault(h => h.Id == item.Id);
            hotspot?.SetSortOrder(item.SortOrder);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Showrooms: true), cancellationToken);
    }

    private async Task<ShowroomDetailDto> MapToDetailDtoAsync(Showroom s, CancellationToken cancellationToken)
    {
        var prodIds = s.Hotspots.Where(h => h.ProductId.HasValue).Select(h => h.ProductId!.Value).Distinct().ToList();
        var productsInfo = await dbContext.Products.AsNoTracking()
            .Where(p => prodIds.Contains(p.Id))
            .Select(p => new { p.Id, p.NormalizedSku, Name = p.Translations.Where(t => t.LanguageCode == "tr").Select(t => t.Name).FirstOrDefault() })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var hotspotDtos = s.Hotspots.Select(h => new ShowroomHotspotDto(
            h.Id, h.ShowroomId, h.ProductId,
            h.ProductId.HasValue && productsInfo.TryGetValue(h.ProductId.Value, out var pi) ? pi.NormalizedSku : null,
            h.ProductId.HasValue && productsInfo.TryGetValue(h.ProductId.Value, out var pi2) ? pi2.Name : null,
            h.TitleTr, h.TitleEn, h.DescriptionTr, h.DescriptionEn,
            h.PositionX, h.PositionY, h.PositionZ, h.SceneIdentifier,
            h.SortOrder, h.IsActive, h.RowVersion)).ToList();

        var translationDtos = s.Translations.Select(t => new ShowroomTranslationDto(
            t.LanguageCode, t.Title, t.Slug, t.ShortDescription, t.LongDescription,
            t.MetaTitle, t.MetaDescription, t.OpenGraphTitle, t.OpenGraphDescription, t.OpenGraphMediaAssetId)).ToList();

        var mediaDtos = s.Media.Select(m => new ShowroomMediaDto(
            m.Id, m.MediaAssetId, m.SortOrder, m.CaptionTr, m.CaptionEn, m.CreatedAt)).ToList();

        return new ShowroomDetailDto(
            s.Id, s.CoverMediaAssetId, s.VirtualTourUrl, s.EmbedCode,
            s.WorkflowStatus, s.PublishAt, s.PublishedAt, s.SortOrder,
            s.IsFeatured, s.CreatedAt, s.RowVersion,
            translationDtos, mediaDtos, hotspotDtos);
    }
}
