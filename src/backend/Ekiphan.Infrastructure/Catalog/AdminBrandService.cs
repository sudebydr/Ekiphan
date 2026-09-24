using Ekiphan.Application.Catalog;
using Ekiphan.Domain.Catalog;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Catalog;

internal sealed class AdminBrandService(EkiphanDbContext dbContext)
    : IAdminBrandService
{
    public async Task<AdminBrandPage> GetPageAsync(
        int page,
        int pageSize,
        string languageCode,
        string? search,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(page, pageSize);
        var language = ValidateLanguage(languageCode);
        var brands = dbContext.Brands
            .AsNoTracking()
            .Where(brand => brand.Translations.Any(
                translation => translation.LanguageCode == language));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (term.Length > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(search));
            }

            brands = brands.Where(brand => brand.Name.Contains(term));
        }

        var totalCount = await brands.CountAsync(cancellationToken);
        var items = await brands
            .OrderBy(brand => brand.SortOrder)
            .ThenBy(brand => brand.Name)
            .ThenBy(brand => brand.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(brand => new AdminBrandSummary(
                brand.Id,
                brand.Name,
                brand.Translations
                    .Where(translation =>
                        translation.LanguageCode == language)
                    .Select(translation => translation.Slug)
                    .Single(),
                brand.WebsiteUrl,
                brand.IsPublished,
                brand.SortOrder))
            .ToListAsync(cancellationToken);
        return new AdminBrandPage(items, page, pageSize, totalCount);
    }

    public Task<AdminBrandDetail?> GetAsync(
        Guid brandId,
        CancellationToken cancellationToken = default) =>
        Project(dbContext.Brands
                .AsNoTracking()
                .Where(brand => brand.Id == brandId))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<AdminBrandDetail> CreateAsync(
        SaveAdminBrandCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        var brand = new Brand(
            Guid.NewGuid(),
            command.Name,
            command.WebsiteUrl,
            command.SortOrder);
        Apply(brand, command);
        dbContext.Brands.Add(brand);
        await SaveAsync(cancellationToken);
        return await GetRequiredAsync(brand.Id, cancellationToken);
    }

    public async Task<AdminBrandDetail?> UpdateAsync(
        Guid brandId,
        SaveAdminBrandCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        var brand = await dbContext.Brands
            .Include(item => item.Translations)
            .SingleOrDefaultAsync(
                item => item.Id == brandId,
                cancellationToken);
        if (brand is null)
        {
            return null;
        }

        brand.Update(command.Name, command.WebsiteUrl, command.SortOrder);
        Apply(brand, command);
        await SaveAsync(cancellationToken);
        return await GetRequiredAsync(brand.Id, cancellationToken);
    }

    public async Task<bool> ArchiveAsync(
        Guid brandId,
        CancellationToken cancellationToken = default)
    {
        var brand = await dbContext.Brands.SingleOrDefaultAsync(
            item => item.Id == brandId,
            cancellationToken);
        if (brand is null)
        {
            return false;
        }

        if (!brand.IsPublished)
        {
            return true;
        }

        var hasProducts = await dbContext.Products
            .AsNoTracking()
            .AnyAsync(
                product => !product.IsDeleted && product.BrandId == brandId,
                cancellationToken);
        if (hasProducts)
        {
            throw new AdminBrandConflictException(
                "A brand assigned to active products cannot be archived.");
        }

        brand.SetPublished(false);
        await SaveAsync(cancellationToken);
        return true;
    }

    private static IQueryable<AdminBrandDetail> Project(
        IQueryable<Brand> brands) =>
        brands.Select(brand => new AdminBrandDetail(
                brand.Id,
                brand.Name,
                brand.WebsiteUrl,
                brand.IsPublished,
                brand.SortOrder,
                brand.Translations
                    .OrderBy(translation => translation.LanguageCode)
                    .Select(translation => new AdminBrandTranslation(
                        translation.LanguageCode,
                        translation.Description,
                        translation.Slug))
                    .ToArray(),
                brand.CreatedAt,
                brand.UpdatedAt));

    private async Task<AdminBrandDetail> GetRequiredAsync(
        Guid brandId,
        CancellationToken cancellationToken) =>
        await GetAsync(brandId, cancellationToken) ??
        throw new InvalidOperationException("The saved brand could not be reloaded.");

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new AdminBrandConflictException(
                "The brand name or translated slug is already in use.");
        }
    }

    private static void Apply(Brand brand, SaveAdminBrandCommand command)
    {
        var languages = command.Translations
            .Select(item => item.LanguageCode.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
        foreach (var translation in command.Translations)
        {
            brand.SetTranslation(
                translation.LanguageCode,
                translation.Description,
                translation.Slug);
        }

        foreach (var language in brand.Translations
            .Select(item => item.LanguageCode)
            .Where(language => !languages.Contains(language))
            .ToArray())
        {
            brand.RemoveTranslation(language);
        }

        brand.SetPublished(command.IsPublished);
    }

    private static void Validate(SaveAdminBrandCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Translations is null ||
            command.Translations.Count is < 1 or > 2)
        {
            throw new ArgumentException(
                "One or two translations are required.",
                nameof(command));
        }

        var languages = command.Translations
            .Select(item => item.LanguageCode?.ToLowerInvariant())
            .ToArray();
        if (languages.Any(language => language is not ("tr" or "en")) ||
            languages.Distinct(StringComparer.Ordinal).Count() !=
            languages.Length)
        {
            throw new ArgumentException(
                "Translations must use unique tr or en language codes.",
                nameof(command));
        }
    }

    private static string ValidateLanguage(string languageCode) =>
        languageCode is "tr" or "en"
            ? languageCode
            : throw new ArgumentOutOfRangeException(nameof(languageCode));

    private static void ValidatePage(int page, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 100);
    }
}
