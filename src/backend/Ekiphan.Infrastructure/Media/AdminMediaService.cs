using Ekiphan.Application.Catalog;
using Ekiphan.Application.Media;
using Ekiphan.Domain.Media;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Media;

internal sealed class AdminMediaService(
    EkiphanDbContext dbContext,
    IPublicMediaUrlResolver mediaUrlResolver,
    IMediaFileStorage storage,
    TimeProvider timeProvider)
    : IAdminMediaService
{
    public async Task<AdminMediaLibrary> GetAsync(
        string? search,
        string? assetType,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(page));
        }

        var assets = dbContext.MediaAssets
            .AsNoTracking()
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (term.Length > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(search));
            }

            assets = assets.Where(item =>
                (item.OriginalFileName != null &&
                 item.OriginalFileName.Contains(term)) ||
                item.Translations.Any(text => text.Title.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(assetType))
        {
            if (int.TryParse(assetType, out _) ||
                !Enum.TryParse<MediaAssetType>(assetType, true, out var parsedType) ||
                !Enum.IsDefined(parsedType))
            {
                throw new ArgumentException("Media asset type is invalid.");
            }
            assets = assets.Where(item => item.AssetType == parsedType);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (int.TryParse(status, out _) ||
                !Enum.TryParse<MediaStatus>(status, true, out var parsedStatus) ||
                !Enum.IsDefined(parsedStatus))
            {
                throw new ArgumentException("Media status is invalid.");
            }
            assets = assets.Where(item => item.Status == parsedStatus);
        }

        var totalCount = await assets.CountAsync(cancellationToken);
        var rawAssets = await assets
            .OrderByDescending(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new
            {
                item.Id,
                AssetType = item.AssetType.ToString(),
                Status = item.Status.ToString(),
                item.OriginalFileName,
                item.MimeType,
                item.FileSizeBytes,
                item.StorageKey,
                item.ExternalUrl,
                Translations = item.Translations
                    .OrderBy(text => text.LanguageCode)
                    .Select(text => new AdminMediaTranslationInput(
                        text.LanguageCode,
                        text.Title,
                        text.AltText,
                        text.Description))
                    .ToArray(),
                item.CreatedAt,
            })
            .ToListAsync(cancellationToken);
        var details = rawAssets.Select(item => new AdminMediaAssetDetail(
            item.Id,
            item.AssetType,
            item.Status,
            item.OriginalFileName,
            item.MimeType,
            item.FileSizeBytes,
            item.ExternalUrl ?? mediaUrlResolver.Resolve(item.StorageKey),
            item.Translations,
            item.CreatedAt)).ToArray();

        var assetIds = details.Select(item => item.Id).ToArray();

        var productAssignments = await dbContext.ProductMedia
            .AsNoTracking()
            .Where(item => assetIds.Contains(item.MediaAssetId))
            .Select(item => new AdminMediaAssignmentDetail(
                "product",
                item.ProductId,
                item.MediaAssetId,
                item.Role.ToString(),
                item.IsDefault,
                item.SortOrder))
            .ToListAsync(cancellationToken);
        var brandAssignments = await dbContext.BrandMedia
            .AsNoTracking()
            .Where(item => assetIds.Contains(item.MediaAssetId))
            .Select(item => new AdminMediaAssignmentDetail(
                "brand",
                item.BrandId,
                item.MediaAssetId,
                item.Role.ToString(),
                false,
                item.SortOrder))
            .ToListAsync(cancellationToken);
        var categoryAssignments = await dbContext.CategoryMedia
            .AsNoTracking()
            .Where(item => assetIds.Contains(item.MediaAssetId))
            .Select(item => new AdminMediaAssignmentDetail(
                "category",
                item.CategoryId,
                item.MediaAssetId,
                item.Role.ToString(),
                false,
                0))
            .ToListAsync(cancellationToken);
        var variantAssignments = await dbContext.ProductVariants
            .AsNoTracking()
            .Where(item => item.MediaAssetId.HasValue &&
                assetIds.Contains(item.MediaAssetId.Value))
            .Select(item => new AdminMediaAssignmentDetail(
                "variant", item.Id, item.MediaAssetId!.Value,
                "VariantImage", false, item.SortOrder))
            .ToListAsync(cancellationToken);
        var heroDesktopAssignments = await dbContext.HomepageHeroes
            .AsNoTracking()
            .Where(item => item.DesktopMediaId.HasValue &&
                assetIds.Contains(item.DesktopMediaId.Value))
            .Select(item => new AdminMediaAssignmentDetail(
                "homepage-hero", item.Id, item.DesktopMediaId!.Value,
                "Desktop", false, item.SortOrder))
            .ToListAsync(cancellationToken);
        var heroMobileAssignments = await dbContext.HomepageHeroes
            .AsNoTracking()
            .Where(item => item.MobileMediaId.HasValue &&
                assetIds.Contains(item.MobileMediaId.Value))
            .Select(item => new AdminMediaAssignmentDetail(
                "homepage-hero", item.Id, item.MobileMediaId!.Value,
                "Mobile", false, item.SortOrder))
            .ToListAsync(cancellationToken);
        var galleryAssignments = await dbContext.GalleryItems
            .AsNoTracking()
            .Where(item => assetIds.Contains(item.MediaAssetId))
            .Select(item => new AdminMediaAssignmentDetail(
                "gallery", item.Id, item.MediaAssetId,
                "Image", false, item.SortOrder))
            .ToListAsync(cancellationToken);
        var pressCoverAssignments = await dbContext.PressReleases
            .AsNoTracking()
            .Where(item => item.CoverMediaId.HasValue &&
                assetIds.Contains(item.CoverMediaId.Value))
            .Select(item => new AdminMediaAssignmentDetail(
                "press-release", item.Id, item.CoverMediaId!.Value,
                "Cover", false, 0))
            .ToListAsync(cancellationToken);
        var pressAttachmentAssignments = await dbContext.PressReleases
            .AsNoTracking()
            .Where(item => item.AttachmentMediaId.HasValue &&
                assetIds.Contains(item.AttachmentMediaId.Value))
            .Select(item => new AdminMediaAssignmentDetail(
                "press-release", item.Id, item.AttachmentMediaId!.Value,
                "Attachment", false, 0))
            .ToListAsync(cancellationToken);
        var products = await dbContext.Products
            .AsNoTracking()
            .Where(item => !item.IsDeleted && item.Translations.Any(text =>
                text.LanguageCode == "tr"))
            .OrderBy(item => item.SKU)
            .Select(item => new AdminMediaTarget(
                item.Id,
                item.Translations
                    .Where(text => text.LanguageCode == "tr")
                    .Select(text => text.Name)
                    .Single(),
                item.SKU))
            .Take(500)
            .ToListAsync(cancellationToken);
        var brands = await dbContext.Brands
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .Select(item => new AdminMediaTarget(
                item.Id,
                item.Name,
                null))
            .Take(500)
            .ToListAsync(cancellationToken);
        var categories = await dbContext.Categories
            .AsNoTracking()
            .Where(item => item.Translations.Any(text =>
                text.LanguageCode == "tr"))
            .OrderBy(item => item.SortOrder)
            .Select(item => new AdminMediaTarget(
                item.Id,
                item.Translations
                    .Where(text => text.LanguageCode == "tr")
                    .Select(text => text.Name)
                    .Single(),
                null))
            .Take(500)
            .ToListAsync(cancellationToken);
        return new AdminMediaLibrary(
            details,
            productAssignments
                .Concat(brandAssignments)
                .Concat(categoryAssignments)
                .Concat(variantAssignments)
                .Concat(heroDesktopAssignments)
                .Concat(heroMobileAssignments)
                .Concat(galleryAssignments)
                .Concat(pressCoverAssignments)
                .Concat(pressAttachmentAssignments)
                .ToArray(),
            products,
            brands,
            categories,
            page,
            pageSize,
            totalCount);
    }

    public async Task<AdminMediaAssetDetail> CreateExternalVideoAsync(
        CreateExternalVideoCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command.Translations);
        var asset = MediaAsset.CreateExternalVideo(
            Guid.NewGuid(),
            command.ExternalUrl);
        Apply(asset, command.Translations);
        dbContext.MediaAssets.Add(asset);
        await SaveAsync(cancellationToken);
        return await GetAssetAsync(asset.Id, cancellationToken);
    }

    public async Task<AdminMediaAssetDetail?> UpdateAssetAsync(
        Guid mediaAssetId,
        SaveAdminMediaCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command.Translations);
        var asset = await dbContext.MediaAssets
            .Include(item => item.Translations)
            .SingleOrDefaultAsync(item => item.Id == mediaAssetId, cancellationToken);
        if (asset is null)
        {
            return null;
        }

        Apply(asset, command.Translations);
        if (command.Archive && asset.Status != MediaStatus.Archived)
        {
            if (await IsInUseAsync(mediaAssetId, cancellationToken))
            {
                throw new AdminMediaConflictException(
                    "Media in use cannot be archived. Remove every assignment first.");
            }
            asset.Archive(timeProvider.GetUtcNow());
        }

        await SaveAsync(cancellationToken);
        return await GetAssetAsync(asset.Id, cancellationToken);
    }

    public async Task<bool> DeletePdfAsync(
        Guid mediaAssetId,
        CancellationToken cancellationToken = default)
    {
        var asset = await dbContext.MediaAssets
            .SingleOrDefaultAsync(item => item.Id == mediaAssetId, cancellationToken);
        if (asset is null) return false;
        if (asset.AssetType != MediaAssetType.Pdf)
            throw new ArgumentException("Only PDF media can be deleted from this endpoint.");
        if (await IsInUseAsync(mediaAssetId, cancellationToken))
            throw new AdminMediaConflictException(
                "PDF in use cannot be deleted. Remove every assignment first.");

        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken);
        dbContext.MediaAssets.Remove(asset);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(asset.StorageKey))
            await storage.DeleteAsync(asset.StorageKey, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<AdminMediaAssetDetail?> SetPdfStatusAsync(
        Guid mediaAssetId,
        SetAdminMediaStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var asset = await dbContext.MediaAssets
            .SingleOrDefaultAsync(item => item.Id == mediaAssetId, cancellationToken);
        if (asset is null) return null;
        if (asset.AssetType != MediaAssetType.Pdf)
            throw new ArgumentException("Only PDF media status can be changed from this endpoint.");

        if (command.Active)
            asset.Activate();
        else if (asset.Status != MediaStatus.Archived)
        {
            if (await IsInUseAsync(mediaAssetId, cancellationToken))
                throw new AdminMediaConflictException(
                    "PDF in use cannot be made inactive. Remove every assignment first.");
            asset.Archive(timeProvider.GetUtcNow());
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAssetAsync(asset.Id, cancellationToken);
    }

    public async Task<AdminMediaAssignmentDetail> SaveAssignmentAsync(
        SaveMediaAssignmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var asset = await dbContext.MediaAssets
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == command.MediaAssetId,
                cancellationToken)
            ?? throw new ArgumentException("The selected media asset does not exist.");
        if (asset.Status != MediaStatus.Active)
        {
            throw new ArgumentException("Archived media cannot be assigned.");
        }

        var result = command.TargetType.Trim().ToLowerInvariant() switch
        {
            "product" => await SaveProductAsync(command, asset, cancellationToken),
            "brand" => await SaveBrandAsync(command, asset, cancellationToken),
            "category" => await SaveCategoryAsync(command, asset, cancellationToken),
            _ => throw new ArgumentException("Media target type is invalid."),
        };
        await SaveAsync(cancellationToken);
        return result;
    }

    public async Task<bool> RemoveAssignmentAsync(
        string targetType,
        Guid targetId,
        Guid mediaAssetId,
        string role,
        CancellationToken cancellationToken = default)
    {
        switch (targetType.Trim().ToLowerInvariant())
        {
            case "product" when Enum.TryParse<ProductMediaRole>(
                role, true, out var productRole):
                var product = await dbContext.ProductMedia.SingleOrDefaultAsync(
                    item => item.ProductId == targetId &&
                        item.MediaAssetId == mediaAssetId &&
                        item.Role == productRole,
                    cancellationToken);
                if (product is null) return false;
                dbContext.ProductMedia.Remove(product);
                break;
            case "brand" when Enum.TryParse<BrandMediaRole>(
                role, true, out var brandRole):
                var brand = await dbContext.BrandMedia.SingleOrDefaultAsync(
                    item => item.BrandId == targetId &&
                        item.MediaAssetId == mediaAssetId &&
                        item.Role == brandRole,
                    cancellationToken);
                if (brand is null) return false;
                dbContext.BrandMedia.Remove(brand);
                break;
            case "category" when Enum.TryParse<CategoryMediaRole>(
                role, true, out var categoryRole):
                var category = await dbContext.CategoryMedia.SingleOrDefaultAsync(
                    item => item.CategoryId == targetId &&
                        item.MediaAssetId == mediaAssetId &&
                        item.Role == categoryRole,
                    cancellationToken);
                if (category is null) return false;
                dbContext.CategoryMedia.Remove(category);
                break;
            default:
                throw new ArgumentException("Media target type or role is invalid.");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<AdminMediaAssignmentDetail> SaveProductAsync(
        SaveMediaAssignmentCommand command,
        MediaAsset asset,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ProductMediaRole>(
                command.Role,
                true,
                out var role) ||
            !ValidProductType(role, asset.AssetType))
        {
            throw new ArgumentException(
                "The media type is incompatible with the product role.");
        }

        if (!await dbContext.Products.AnyAsync(
                item => item.Id == command.TargetId,
                cancellationToken))
        {
            throw new ArgumentException("The selected product does not exist.");
        }

        if (command.IsDefault)
        {
            var defaults = await dbContext.ProductMedia
                .Where(item => item.ProductId == command.TargetId &&
                    item.IsDefault)
                .ToListAsync(cancellationToken);
            foreach (var item in defaults)
            {
                item.Update(false, item.SortOrder);
            }
        }

        var assignment = await dbContext.ProductMedia.SingleOrDefaultAsync(
            item => item.ProductId == command.TargetId &&
                item.MediaAssetId == command.MediaAssetId &&
                item.Role == role,
            cancellationToken);
        if (assignment is null)
        {
            assignment = new ProductMedia(
                command.TargetId,
                command.MediaAssetId,
                role,
                command.IsDefault,
                command.SortOrder);
            dbContext.ProductMedia.Add(assignment);
        }
        else
        {
            assignment.Update(command.IsDefault, command.SortOrder);
        }

        return Detail(command, role.ToString());
    }

    private async Task<AdminMediaAssignmentDetail> SaveBrandAsync(
        SaveMediaAssignmentCommand command,
        MediaAsset asset,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<BrandMediaRole>(
                command.Role,
                true,
                out var role) ||
            !ValidBrandType(role, asset.AssetType))
        {
            throw new ArgumentException(
                "The media type is incompatible with the brand role.");
        }

        if (!await dbContext.Brands.AnyAsync(
                item => item.Id == command.TargetId,
                cancellationToken))
        {
            throw new ArgumentException("The selected brand does not exist.");
        }

        var assignment = await dbContext.BrandMedia.SingleOrDefaultAsync(
            item => item.BrandId == command.TargetId &&
                item.Role == role,
            cancellationToken);
        if (assignment is not null &&
            assignment.MediaAssetId != command.MediaAssetId)
        {
            dbContext.BrandMedia.Remove(assignment);
            assignment = null;
        }

        if (assignment is null)
        {
            assignment = new BrandMedia(
                command.TargetId,
                command.MediaAssetId,
                role,
                command.SortOrder);
            dbContext.BrandMedia.Add(assignment);
        }
        else
        {
            assignment.Update(command.SortOrder);
        }

        return Detail(command, role.ToString());
    }

    private async Task<AdminMediaAssignmentDetail> SaveCategoryAsync(
        SaveMediaAssignmentCommand command,
        MediaAsset asset,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<CategoryMediaRole>(
                command.Role,
                true,
                out var role) ||
            asset.AssetType != MediaAssetType.Image)
        {
            throw new ArgumentException(
                "Category image roles require an image asset.");
        }

        if (!await dbContext.Categories.AnyAsync(
                item => item.Id == command.TargetId,
                cancellationToken))
        {
            throw new ArgumentException("The selected category does not exist.");
        }

        var assignment = await dbContext.CategoryMedia.SingleOrDefaultAsync(
            item => item.CategoryId == command.TargetId &&
                item.Role == role,
            cancellationToken);
        if (assignment is not null)
        {
            assignment.UpdateMedia(command.MediaAssetId);
        }
        else
        {
            dbContext.CategoryMedia.Add(new CategoryMedia(
                command.TargetId,
                command.MediaAssetId,
                role));
        }
        return Detail(command, role.ToString());
    }

    private async Task<AdminMediaAssetDetail> GetAssetAsync(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.MediaAssets.AsNoTracking()
            .Where(asset => asset.Id == assetId)
            .Select(asset => new
            {
                asset.Id,
                AssetType = asset.AssetType.ToString(),
                Status = asset.Status.ToString(),
                asset.OriginalFileName,
                asset.MimeType,
                asset.FileSizeBytes,
                asset.StorageKey,
                asset.ExternalUrl,
                Translations = asset.Translations
                    .OrderBy(text => text.LanguageCode)
                    .Select(text => new AdminMediaTranslationInput(
                        text.LanguageCode, text.Title, text.AltText,
                        text.Description))
                    .ToArray(),
                asset.CreatedAt,
            })
            .SingleAsync(cancellationToken);
        return new AdminMediaAssetDetail(
            item.Id, item.AssetType, item.Status, item.OriginalFileName,
            item.MimeType, item.FileSizeBytes,
            item.ExternalUrl ?? mediaUrlResolver.Resolve(item.StorageKey),
            item.Translations, item.CreatedAt);
    }

    private async Task<bool> IsInUseAsync(
        Guid mediaAssetId,
        CancellationToken cancellationToken) =>
        await dbContext.ProductMedia.AsNoTracking().AnyAsync(
            item => item.MediaAssetId == mediaAssetId, cancellationToken) ||
        await dbContext.BrandMedia.AsNoTracking().AnyAsync(
            item => item.MediaAssetId == mediaAssetId, cancellationToken) ||
        await dbContext.CategoryMedia.AsNoTracking().AnyAsync(
            item => item.MediaAssetId == mediaAssetId, cancellationToken) ||
        await dbContext.ProductVariants.AsNoTracking().AnyAsync(
            item => item.MediaAssetId == mediaAssetId, cancellationToken) ||
        await dbContext.HomepageHeroes.AsNoTracking().AnyAsync(
            item => item.DesktopMediaId == mediaAssetId ||
                item.MobileMediaId == mediaAssetId, cancellationToken) ||
        await dbContext.GalleryItems.AsNoTracking().AnyAsync(
            item => item.MediaAssetId == mediaAssetId, cancellationToken) ||
        await dbContext.PressReleases.AsNoTracking().AnyAsync(
            item => item.CoverMediaId == mediaAssetId ||
                item.AttachmentMediaId == mediaAssetId, cancellationToken);

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new AdminMediaConflictException(
                "The media assignment conflicts with an existing role or default.");
        }
    }

    private static void Validate(
        IReadOnlyList<AdminMediaTranslationInput>? translations)
    {
        if (translations is null || translations.Count is < 1 or > 2)
        {
            throw new ArgumentException("One or two translations are required.");
        }

        var languages = translations
            .Select(item => item.LanguageCode?.ToLowerInvariant())
            .ToArray();
        if (languages.Any(item => item is not ("tr" or "en")) ||
            languages.Distinct(StringComparer.Ordinal).Count() != languages.Length)
        {
            throw new ArgumentException(
                "Translations must use unique tr or en language codes.");
        }
    }

    private static void Apply(
        MediaAsset asset,
        IReadOnlyList<AdminMediaTranslationInput> translations)
    {
        var languages = translations
            .Select(item => item.LanguageCode.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
        foreach (var translation in translations)
        {
            asset.SetTranslation(
                translation.LanguageCode,
                translation.Title,
                translation.AltText,
                translation.Description);
        }

        foreach (var language in asset.Translations
            .Select(item => item.LanguageCode)
            .Where(item => !languages.Contains(item))
            .ToArray())
        {
            asset.RemoveTranslation(language);
        }
    }

    private static bool ValidProductType(
        ProductMediaRole role,
        MediaAssetType type) =>
        role switch
        {
            ProductMediaRole.GalleryImage => type == MediaAssetType.Image,
            ProductMediaRole.PdfCatalog => type == MediaAssetType.Pdf,
            ProductMediaRole.Document =>
                type is MediaAssetType.Document or MediaAssetType.Pdf,
            ProductMediaRole.Video => type == MediaAssetType.ExternalVideo,
            _ => false,
        };

    private static bool ValidBrandType(
        BrandMediaRole role,
        MediaAssetType type) =>
        role switch
        {
            BrandMediaRole.Logo => type == MediaAssetType.Image,
            BrandMediaRole.PdfCatalog => type == MediaAssetType.Pdf,
            _ => false,
        };

    private static AdminMediaAssignmentDetail Detail(
        SaveMediaAssignmentCommand command,
        string role) =>
        new(
            command.TargetType.Trim().ToLowerInvariant(),
            command.TargetId,
            command.MediaAssetId,
            role,
            command.IsDefault,
            command.SortOrder);
}
