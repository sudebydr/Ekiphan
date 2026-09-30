using Ekiphan.Application.Catalog;
using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.Media;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Catalog;

internal sealed class AdminCategoryService(EkiphanDbContext dbContext)
    : IAdminCategoryService
{
    private const int MaximumCategoryDepth = 5;

    public async Task<AdminCatalogStructure> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var sectionQuery = dbContext.ProductSections
            .AsNoTracking()
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Code);
        var sections = await ProjectSections(sectionQuery)
            .ToListAsync(cancellationToken);
        var categoryQuery = dbContext.Categories
            .AsNoTracking()
            .OrderBy(item => item.ProductSectionId)
            .ThenBy(item => item.ParentId)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Id);
        var categories = await ProjectCategories(categoryQuery)
            .ToListAsync(cancellationToken);
        var productCounts = await dbContext.Set<ProductCategory>()
            .AsNoTracking()
            .Join(
                dbContext.Products.AsNoTracking().Where(product => !product.IsDeleted),
                assignment => assignment.ProductId,
                product => product.Id,
                (assignment, _) => assignment.CategoryId)
            .GroupBy(categoryId => categoryId)
            .Select(group => new { CategoryId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.CategoryId, item => item.Count, cancellationToken);
        categories = categories
            .Select(category => category with
            {
                ProductCount = productCounts.GetValueOrDefault(category.Id),
            })
            .ToList();
        return new AdminCatalogStructure(sections, categories);
    }

    public async Task<AdminSectionDetail> CreateSectionAsync(
        SaveAdminSectionCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        var section = new ProductSection(
            Guid.NewGuid(),
            command.Code,
            command.SortOrder);
        Apply(section, command);
        dbContext.ProductSections.Add(section);
        await SaveAsync(cancellationToken);
        return await GetRequiredSectionAsync(section.Id, cancellationToken);
    }

    public async Task<AdminSectionDetail?> UpdateSectionAsync(
        Guid sectionId,
        SaveAdminSectionCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        var section = await dbContext.ProductSections
            .Include(item => item.Translations)
            .SingleOrDefaultAsync(item => item.Id == sectionId, cancellationToken);
        if (section is null)
        {
            return null;
        }

        section.Update(command.Code, command.SortOrder);
        Apply(section, command);
        await SaveAsync(cancellationToken);
        return await GetRequiredSectionAsync(section.Id, cancellationToken);
    }

    public async Task<AdminCategoryDetail> CreateCategoryAsync(
        SaveAdminCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        await ValidateSeoMediaAsync(command.Translations, cancellationToken);
        await ValidateLocationAsync(
            null,
            command.ProductSectionId,
            command.ParentId,
            cancellationToken);
        var category = new Category(
            Guid.NewGuid(),
            command.ProductSectionId,
            command.ParentId,
            command.SortOrder);
        Apply(category, command);
        dbContext.Categories.Add(category);
        await SaveAsync(cancellationToken);
        return await GetRequiredCategoryAsync(category.Id, cancellationToken);
    }

    public async Task<AdminCategoryDetail?> UpdateCategoryAsync(
        Guid categoryId,
        SaveAdminCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        await ValidateSeoMediaAsync(command.Translations, cancellationToken);
        var category = await dbContext.Categories
            .Include(item => item.Translations)
            .SingleOrDefaultAsync(item => item.Id == categoryId, cancellationToken);
        if (category is null)
        {
            return null;
        }

        await ValidateLocationAsync(
            categoryId,
            command.ProductSectionId,
            command.ParentId,
            cancellationToken);
        category.Update(
            command.ProductSectionId,
            command.ParentId,
            command.SortOrder);
        Apply(category, command);
        await SaveAsync(cancellationToken);
        return await GetRequiredCategoryAsync(category.Id, cancellationToken);
    }

    public async Task<bool> ArchiveCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories.SingleOrDefaultAsync(
            item => item.Id == categoryId,
            cancellationToken);
        if (category is null)
        {
            return false;
        }

        if (!category.IsPublished)
        {
            return true;
        }

        var hasPublishedChildren = await dbContext.Categories
            .AsNoTracking()
            .AnyAsync(
                item => item.ParentId == categoryId && item.IsPublished,
                cancellationToken);
        if (hasPublishedChildren)
        {
            throw new AdminCategoryConflictException(
                "A category with published child categories cannot be archived.");
        }

        var hasProducts = await dbContext.Products
            .AsNoTracking()
            .AnyAsync(
                product => !product.IsDeleted && product.Categories.Any(
                    assignment => assignment.CategoryId == categoryId),
                cancellationToken);
        if (hasProducts)
        {
            throw new AdminCategoryConflictException(
                "A category assigned to active products cannot be archived.");
        }

        category.SetPublished(false);
        await SaveAsync(cancellationToken);
        return true;
    }

    private static IQueryable<AdminSectionDetail> ProjectSections(
        IQueryable<ProductSection> sections) =>
        sections.Select(section =>
            new AdminSectionDetail(
                section.Id,
                section.Code,
                section.IsPublished,
                section.SortOrder,
                section.Translations
                    .OrderBy(item => item.LanguageCode)
                    .Select(item => new AdminSectionTranslation(
                        item.LanguageCode,
                        item.Name,
                        item.Slug))
                    .ToArray(),
                section.CreatedAt,
                section.UpdatedAt));

    private static IQueryable<AdminCategoryDetail> ProjectCategories(
        IQueryable<Category> categories) =>
        categories.Select(category =>
            new AdminCategoryDetail(
                category.Id,
                category.ProductSectionId,
                category.ParentId,
                category.IsPublished,
                category.SortOrder,
                0,
                category.Translations
                    .OrderBy(item => item.LanguageCode)
                    .Select(item => new AdminCategoryTranslation(
                        item.LanguageCode,
                        item.Name,
                        item.Slug,
                        item.Description,
                        item.MetaTitle,
                        item.MetaDescription,
                        item.CanonicalUrl,
                        item.NoIndex,
                        item.NoFollow,
                        item.OpenGraphTitle,
                        item.OpenGraphDescription,
                        item.OpenGraphImageMediaId,
                        null))
                    .ToArray(),
                category.CreatedAt,
                category.UpdatedAt));

    private async Task ValidateLocationAsync(
        Guid? categoryId,
        Guid sectionId,
        Guid? parentId,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.ProductSections.AnyAsync(
                item => item.Id == sectionId,
                cancellationToken))
        {
            throw new ArgumentException(
                "The selected product section does not exist.",
                nameof(sectionId));
        }

        var categories = await dbContext.Categories
            .AsNoTracking()
            .Select(item => new CategoryLocation(
                item.Id,
                item.ParentId,
                item.ProductSectionId))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        if (categoryId.HasValue)
        {
            var hasChildrenInAnotherSection = categories.Values.Any(item =>
                item.ParentId == categoryId &&
                item.ProductSectionId != sectionId);
            if (hasChildrenInAnotherSection)
            {
                throw new ArgumentException(
                    "A category with child categories cannot move to another product section.",
                    nameof(sectionId));
            }
        }

        var parentDepth = 0;
        CategoryLocation? parent = null;
        if (parentId.HasValue &&
            !categories.TryGetValue(parentId.Value, out parent))
        {
            throw new ArgumentException(
                "The selected parent category does not exist.",
                nameof(parentId));
        }

        if (parentId.HasValue && parent!.ProductSectionId != sectionId)
        {
            throw new ArgumentException(
                "A parent category must belong to the same product section.",
                nameof(parentId));
        }

        var currentId = parentId;
        var visited = new HashSet<Guid>();
        while (currentId.HasValue)
        {
            if (currentId == categoryId)
            {
                throw new ArgumentException(
                    "The selected parent would create a category cycle.",
                    nameof(parentId));
            }

            if (!visited.Add(currentId.Value) ||
                !categories.TryGetValue(currentId.Value, out var current))
            {
                throw new ArgumentException(
                    "The category hierarchy is invalid.",
                    nameof(parentId));
            }

            parentDepth++;
            currentId = current.ParentId;
        }

        var subtreeHeight = categoryId.HasValue
            ? GetSubtreeHeight(categoryId.Value, categories)
            : 1;
        if (parentDepth + subtreeHeight > MaximumCategoryDepth)
        {
            throw new ArgumentException(
                $"A category hierarchy cannot exceed {MaximumCategoryDepth} levels.",
                nameof(parentId));
        }
    }

    private async Task ValidateSeoMediaAsync(
        IReadOnlyList<AdminCategoryTranslationInput> translations,
        CancellationToken cancellationToken)
    {
        var ids = translations.Where(item => item.OpenGraphImageMediaId.HasValue)
            .Select(item => item.OpenGraphImageMediaId!.Value).Distinct().ToArray();
        if (ids.Length == 0) return;
        var count = await dbContext.MediaAssets.CountAsync(item =>
            ids.Contains(item.Id) && item.Status == MediaStatus.Active &&
            item.AssetType == MediaAssetType.Image, cancellationToken);
        if (count != ids.Length)
            throw new ArgumentException("Open Graph images must reference active image media.");
    }

    private static int GetSubtreeHeight(
        Guid categoryId,
        IReadOnlyDictionary<Guid, CategoryLocation> categories)
    {
        var visiting = new HashSet<Guid>();
        var children = categories.Values
            .Where(item => item.ParentId.HasValue)
            .GroupBy(item => item.ParentId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Id).ToArray());

        int Visit(Guid id)
        {
            if (!visiting.Add(id))
            {
                throw new ArgumentException("The category hierarchy contains a cycle.");
            }

            var maximumChildHeight = 0;
            foreach (var childId in children.GetValueOrDefault(id, []))
            {
                maximumChildHeight = Math.Max(
                    maximumChildHeight,
                    Visit(childId));
            }

            visiting.Remove(id);
            return maximumChildHeight + 1;
        }

        return Visit(categoryId);
    }

    private sealed record CategoryLocation(
        Guid Id,
        Guid? ParentId,
        Guid ProductSectionId);

    private async Task<AdminSectionDetail> GetRequiredSectionAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await ProjectSections(dbContext.ProductSections
                .AsNoTracking()
                .Where(item => item.Id == id))
            .SingleAsync(cancellationToken);

    private async Task<AdminCategoryDetail> GetRequiredCategoryAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await ProjectCategories(dbContext.Categories
                .AsNoTracking()
                .Where(item => item.Id == id))
            .SingleAsync(cancellationToken);

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new AdminCategoryConflictException(
                "The code or translated slug is already in use.");
        }
    }

    private static void Apply(
        ProductSection section,
        SaveAdminSectionCommand command)
    {
        var languages = command.Translations
            .Select(item => item.LanguageCode.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
        foreach (var translation in command.Translations)
        {
            section.SetTranslation(
                translation.LanguageCode,
                translation.Name,
                translation.Slug);
        }

        foreach (var language in section.Translations
            .Select(item => item.LanguageCode)
            .Where(item => !languages.Contains(item))
            .ToArray())
        {
            section.RemoveTranslation(language);
        }

        section.SetPublished(command.IsPublished);
    }

    private static void Apply(
        Category category,
        SaveAdminCategoryCommand command)
    {
        var languages = command.Translations
            .Select(item => item.LanguageCode.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
        foreach (var translation in command.Translations)
        {
            category.SetTranslation(
                translation.LanguageCode,
                translation.Name,
                translation.Slug,
                translation.Description,
                translation.MetaTitle,
                translation.MetaDescription,
                translation.CanonicalUrl,
                translation.NoIndex,
                translation.NoFollow,
                translation.OpenGraphTitle,
                translation.OpenGraphDescription,
                translation.OpenGraphImageMediaId);
        }

        foreach (var language in category.Translations
            .Select(item => item.LanguageCode)
            .Where(item => !languages.Contains(item))
            .ToArray())
        {
            category.RemoveTranslation(language);
        }

        category.SetPublished(command.IsPublished);
    }

    private static void Validate(SaveAdminSectionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateTranslations(command.Translations);
    }

    private static void Validate(SaveAdminCategoryCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.ProductSectionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product section identifier cannot be empty.",
                nameof(command));
        }

        ValidateTranslations(command.Translations);
    }

    private static void ValidateTranslations<T>(
        IReadOnlyList<T>? translations)
        where T : notnull
    {
        if (translations is null || translations.Count is < 1 or > 2)
        {
            throw new ArgumentException("One or two translations are required.");
        }

        var languages = translations.Select(item => item switch
        {
            AdminSectionTranslationInput value => value.LanguageCode,
            AdminCategoryTranslationInput value => value.LanguageCode,
            _ => string.Empty,
        }).Select(item => item?.ToLowerInvariant()).ToArray();
        if (languages.Any(item => item is not ("tr" or "en")) ||
            languages.Distinct(StringComparer.Ordinal).Count() != languages.Length)
        {
            throw new ArgumentException(
                "Translations must use unique tr or en language codes.");
        }
    }
}
