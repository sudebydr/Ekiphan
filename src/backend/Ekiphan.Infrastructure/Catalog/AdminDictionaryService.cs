using Ekiphan.Application.Catalog;
using Ekiphan.Domain.Catalog;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Catalog;

internal sealed class AdminDictionaryService(EkiphanDbContext dbContext)
    : IAdminDictionaryService
{
    public async Task<AdminDictionaryCatalog> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var tagEntities = await TagQuery()
            .OrderBy(item => item.Code)
            .ToListAsync(cancellationToken);
        var tags = tagEntities
            .Select(item => ToTagDetail(item))
            .ToList();
        var units = await ProjectUnits(
                dbContext.Units
                    .AsNoTracking()
                    .OrderBy(item => item.Dimension)
                    .ThenBy(item => item.Code))
            .ToListAsync(cancellationToken);
        var productTagRows = await dbContext.Products
            .AsNoTracking()
            .SelectMany(product => product.Tags.Select(tag => new
            {
                ProductId = product.Id,
                tag.TagId,
                tag.SortOrder,
            }))
            .ToListAsync(cancellationToken);
        var productTags = productTagRows
            .GroupBy(item => item.ProductId)
            .Select(group => new AdminProductTagAssignment(
                group.Key,
                group
                    .OrderBy(item => item.SortOrder)
                    .Select(item => item.TagId)
                    .ToArray()))
            .ToList();
        return new AdminDictionaryCatalog(tags, units, productTags);
    }

    public async Task<AdminTagDetail> CreateTagAsync(
        SaveAdminTagCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        var tag = new Tag(Guid.NewGuid(), command.Code);
        Apply(tag, command);
        dbContext.Tags.Add(tag);
        await SaveAsync(cancellationToken);
        return await GetTagAsync(tag.Id, cancellationToken);
    }

    public async Task<AdminTagDetail?> UpdateTagAsync(
        Guid tagId,
        SaveAdminTagCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        var tag = await dbContext.Tags
            .Include(item => item.Translations)
            .SingleOrDefaultAsync(item => item.Id == tagId, cancellationToken);
        if (tag is null)
        {
            return null;
        }

        tag.Update(command.Code);
        Apply(tag, command);
        await SaveAsync(cancellationToken);
        return await GetTagAsync(tag.Id, cancellationToken);
    }

    public async Task<AdminUnitDefinitionDetail> CreateUnitAsync(
        SaveAdminUnitCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateBaseUnitAsync(null, command, cancellationToken);
        var unit = new UnitDefinition(
            Guid.NewGuid(),
            command.Code,
            command.Symbol,
            command.Dimension,
            command.ConversionFactorToBase,
            command.IsBaseUnit);
        unit.SetActive(command.IsActive);
        dbContext.Units.Add(unit);
        await SaveAsync(cancellationToken);
        return await GetUnitAsync(unit.Id, cancellationToken);
    }

    public async Task<AdminUnitDefinitionDetail?> UpdateUnitAsync(
        Guid unitId,
        SaveAdminUnitCommand command,
        CancellationToken cancellationToken = default)
    {
        var unit = await dbContext.Units.SingleOrDefaultAsync(
            item => item.Id == unitId,
            cancellationToken);
        if (unit is null)
        {
            return null;
        }

        if (!string.Equals(
                unit.Dimension,
                command.Dimension.Trim(),
                StringComparison.OrdinalIgnoreCase) ||
            unit.IsBaseUnit != command.IsBaseUnit)
        {
            throw new ArgumentException(
                "An existing unit dimension and base-unit identity cannot be changed.");
        }

        await ValidateBaseUnitAsync(unitId, command, cancellationToken);
        unit.Update(
            command.Code,
            command.Symbol,
            command.ConversionFactorToBase);
        unit.SetActive(command.IsActive);
        await SaveAsync(cancellationToken);
        return await GetUnitAsync(unit.Id, cancellationToken);
    }

    public async Task<AdminProductTagAssignment?> SaveProductTagsAsync(
        Guid productId,
        IReadOnlyList<Guid> tagIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tagIds);
        if (tagIds.Any(item => item == Guid.Empty) ||
            tagIds.Distinct().Count() != tagIds.Count)
        {
            throw new ArgumentException(
                "Tag identifiers must be non-empty and unique.");
        }

        var product = await dbContext.Products
            .Include(item => item.Tags)
            .SingleOrDefaultAsync(item => item.Id == productId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        var activeTagIds = await dbContext.Tags
            .Where(item => tagIds.Contains(item.Id) && item.IsActive)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        var existingTagIds = product.Tags
            .Select(item => item.TagId)
            .ToHashSet();
        if (tagIds.Any(item =>
                !activeTagIds.Contains(item) && !existingTagIds.Contains(item)))
        {
            throw new ArgumentException(
                "One or more selected tags do not exist or are inactive.");
        }

        product.SetTags(tagIds);
        await SaveAsync(cancellationToken);
        return new AdminProductTagAssignment(productId, tagIds);
    }

    // Ordering or filtering on a constructor-projected DTO (after Select) cannot
    // be translated to SQL. Order/filter on the entity first, then project.
    private IQueryable<Tag> TagQuery() =>
        dbContext.Tags
            .AsNoTracking()
            .Include(item => item.Translations)
            .AsSplitQuery();

    private static IQueryable<AdminUnitDefinitionDetail> ProjectUnits(
        IQueryable<UnitDefinition> units) =>
        units.Select(item =>
            new AdminUnitDefinitionDetail(
                item.Id,
                item.Code,
                item.Symbol,
                item.Dimension,
                item.ConversionFactorToBase,
                item.IsBaseUnit,
                item.IsActive));

    private static AdminTagDetail ToTagDetail(Tag item) =>
        new(
            item.Id,
            item.Code,
            item.IsActive,
            item.Translations
                .OrderBy(text => text.LanguageCode, StringComparer.Ordinal)
                .Select(text => new AdminTagTranslationInput(
                    text.LanguageCode,
                    text.Name,
                    text.Slug))
                .ToArray());

    private async Task<AdminTagDetail> GetTagAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var tag = await TagQuery().SingleAsync(
            item => item.Id == id,
            cancellationToken);
        return ToTagDetail(tag);
    }

    private async Task<AdminUnitDefinitionDetail> GetUnitAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await ProjectUnits(
                dbContext.Units
                    .AsNoTracking()
                    .Where(item => item.Id == id))
            .SingleAsync(cancellationToken);

    private async Task ValidateBaseUnitAsync(
        Guid? unitId,
        SaveAdminUnitCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!command.IsBaseUnit)
        {
            return;
        }

        if (command.ConversionFactorToBase != 1m)
        {
            throw new ArgumentException(
                "A base unit must have a conversion factor of one.");
        }

        var dimension = command.Dimension.Trim().ToUpperInvariant();
        if (await dbContext.Units.AnyAsync(
                item => item.Id != unitId &&
                    item.Dimension == dimension &&
                    item.IsBaseUnit,
                cancellationToken))
        {
            throw new AdminDictionaryConflictException(
                "The unit dimension already has a base unit.");
        }
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new AdminDictionaryConflictException(
                "The tag code, translated slug, unit code, or base unit is already in use.");
        }
    }

    private static void Validate(SaveAdminTagCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Translations is null ||
            command.Translations.Count is < 1 or > 2)
        {
            throw new ArgumentException("One or two translations are required.");
        }

        var languages = command.Translations
            .Select(item => item.LanguageCode?.ToLowerInvariant())
            .ToArray();
        if (languages.Any(item => item is not ("tr" or "en")) ||
            languages.Distinct(StringComparer.Ordinal).Count() != languages.Length)
        {
            throw new ArgumentException(
                "Translations must use unique tr or en language codes.");
        }
    }

    private static void Apply(Tag tag, SaveAdminTagCommand command)
    {
        var languages = command.Translations
            .Select(item => item.LanguageCode.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
        foreach (var text in command.Translations)
        {
            tag.SetTranslation(text.LanguageCode, text.Name, text.Slug);
        }

        foreach (var language in tag.Translations
            .Select(item => item.LanguageCode)
            .Where(item => !languages.Contains(item))
            .ToArray())
        {
            tag.RemoveTranslation(language);
        }

        tag.SetActive(command.IsActive);
    }
}
