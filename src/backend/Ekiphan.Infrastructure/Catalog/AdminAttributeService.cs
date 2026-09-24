using Ekiphan.Application.Catalog;
using Ekiphan.Domain.Catalog;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Catalog;

internal sealed class AdminAttributeService(EkiphanDbContext dbContext)
    : IAdminAttributeService
{
    public async Task<AdminAttributeCatalog> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var attributeEntities = await AttributeQuery()
            .OrderBy(item => item.Code)
            .ToListAsync(cancellationToken);
        var attributes = attributeEntities
            .Select(item => ToAttributeDetail(item))
            .ToList();
        var assignments = await dbContext.CategoryAttributes
            .AsNoTracking()
            .OrderBy(item => item.CategoryId)
            .ThenBy(item => item.SortOrder)
            .Select(item => new AdminCategoryAttributeDetail(
                item.CategoryId,
                item.AttributeId,
                item.IsRequired,
                item.IsFilterable,
                item.IsVisibleOnProduct,
                item.IsVisibleOnComparison,
                item.SortOrder))
            .ToListAsync(cancellationToken);
        var units = await dbContext.Units
            .AsNoTracking()
            .OrderBy(item => item.Dimension)
            .ThenBy(item => item.Code)
            .Select(item => new AdminUnitDetail(
                item.Id,
                item.Code,
                item.Symbol,
                item.Dimension,
                item.IsActive))
            .ToListAsync(cancellationToken);
        return new AdminAttributeCatalog(attributes, assignments, units);
    }

    public async Task<AdminAttributeDetail> CreateAsync(
        SaveAdminAttributeCommand command,
        CancellationToken cancellationToken = default)
    {
        var dataType = Validate(command);
        var attribute = new AttributeDefinition(
            Guid.NewGuid(),
            command.Code,
            dataType,
            command.UnitDimension);
        Apply(attribute, command);
        dbContext.Attributes.Add(attribute);
        await SaveAsync(cancellationToken);
        return await GetRequiredAsync(attribute.Id, cancellationToken);
    }

    public async Task<AdminAttributeDetail?> UpdateAsync(
        Guid id,
        SaveAdminAttributeCommand command,
        CancellationToken cancellationToken = default)
    {
        var dataType = Validate(command);
        var attribute = await dbContext.Attributes
            .Include(item => item.Translations)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (attribute is null)
        {
            return null;
        }

        if (attribute.DataType != dataType)
        {
            throw new ArgumentException(
                "An existing attribute data type cannot be changed.");
        }

        attribute.Update(command.Code, command.UnitDimension);
        Apply(attribute, command);
        await SaveAsync(cancellationToken);
        return await GetRequiredAsync(id, cancellationToken);
    }

    public async Task<AdminAttributeOptionDetail> CreateOptionAsync(
        Guid attributeId,
        AdminAttributeOptionInput command,
        CancellationToken cancellationToken = default)
    {
        Validate(command.Translations);
        var attribute = await dbContext.Attributes
            .Include(item => item.Options)
            .ThenInclude(item => item.Translations)
            .SingleOrDefaultAsync(item => item.Id == attributeId, cancellationToken)
            ?? throw new ArgumentException("The selected attribute does not exist.");
        var option = attribute.AddOption(
            Guid.NewGuid(),
            command.Code,
            command.SortOrder);
        Apply(option, command);
        await SaveAsync(cancellationToken);
        return ToDetail(option);
    }

    public async Task<AdminAttributeOptionDetail?> UpdateOptionAsync(
        Guid optionId,
        AdminAttributeOptionInput command,
        CancellationToken cancellationToken = default)
    {
        Validate(command.Translations);
        var attribute = await dbContext.Attributes
            .Include(item => item.Options)
            .ThenInclude(item => item.Translations)
            .SingleOrDefaultAsync(
                item => item.Options.Any(option => option.Id == optionId),
                cancellationToken);
        if (attribute is null)
        {
            return null;
        }

        var option = attribute.Options.Single(item => item.Id == optionId);
        option.Update(command.Code, command.SortOrder);
        Apply(option, command);
        await SaveAsync(cancellationToken);
        return ToDetail(option);
    }

    public async Task<AdminCategoryAttributeDetail> SaveAssignmentAsync(
        SaveCategoryAttributeCommand command,
        CancellationToken cancellationToken = default)
    {
        var referencesExist =
            await dbContext.Categories.AnyAsync(
                item => item.Id == command.CategoryId,
                cancellationToken) &&
            await dbContext.Attributes.AnyAsync(
                item => item.Id == command.AttributeId,
                cancellationToken);
        if (!referencesExist)
        {
            throw new ArgumentException(
                "The selected category or attribute does not exist.");
        }

        var assignment = await dbContext.CategoryAttributes.SingleOrDefaultAsync(
            item => item.CategoryId == command.CategoryId &&
                item.AttributeId == command.AttributeId,
            cancellationToken);
        if (assignment is null)
        {
            assignment = new CategoryAttributeAssignment(
                command.CategoryId,
                command.AttributeId);
            dbContext.CategoryAttributes.Add(assignment);
        }

        assignment.Update(
            command.IsRequired,
            command.IsFilterable,
            command.IsVisibleOnProduct,
            command.IsVisibleOnComparison,
            command.SortOrder);
        await SaveAsync(cancellationToken);
        return ToDetail(assignment);
    }

    public async Task<bool> RemoveAssignmentAsync(
        Guid categoryId,
        Guid attributeId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await dbContext.CategoryAttributes.SingleOrDefaultAsync(
            item => item.CategoryId == categoryId &&
                item.AttributeId == attributeId,
            cancellationToken);
        if (assignment is null)
        {
            return false;
        }

        dbContext.CategoryAttributes.Remove(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    // Nesting collection projections (attribute -> options -> translations)
    // and then ordering/filtering on the projected DTO cannot be translated
    // to SQL. Load the entities with split queries and map them in memory.
    private IQueryable<AttributeDefinition> AttributeQuery() =>
        dbContext.Attributes
            .AsNoTracking()
            .Include(item => item.Translations)
            .Include(item => item.Options)
            .ThenInclude(item => item.Translations)
            .AsSplitQuery();

    private async Task<AdminAttributeDetail> GetRequiredAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var attribute = await AttributeQuery().SingleAsync(
            item => item.Id == id,
            cancellationToken);
        return ToAttributeDetail(attribute);
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new AdminAttributeConflictException(
                "The attribute or option code is already in use.");
        }
    }

    private static AttributeDataType Validate(
        SaveAdminAttributeCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        Validate(command.Translations);
        return Enum.TryParse<AttributeDataType>(
            command.DataType,
            true,
            out var dataType) &&
            Enum.IsDefined(dataType)
                ? dataType
                : throw new ArgumentException("Attribute data type is invalid.");
    }

    private static void Validate(
        IReadOnlyList<AdminAttributeTranslationInput>? translations)
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
        AttributeDefinition attribute,
        SaveAdminAttributeCommand command)
    {
        var languages = command.Translations
            .Select(item => item.LanguageCode.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
        foreach (var translation in command.Translations)
        {
            attribute.SetTranslation(
                translation.LanguageCode,
                translation.Name);
        }

        foreach (var language in attribute.Translations
            .Select(item => item.LanguageCode)
            .Where(item => !languages.Contains(item))
            .ToArray())
        {
            attribute.RemoveTranslation(language);
        }

        attribute.SetActive(command.IsActive);
    }

    private static void Apply(
        AttributeOption option,
        AdminAttributeOptionInput command)
    {
        var languages = command.Translations
            .Select(item => item.LanguageCode.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
        foreach (var translation in command.Translations)
        {
            option.SetTranslation(
                translation.LanguageCode,
                translation.Name);
        }

        foreach (var language in option.Translations
            .Select(item => item.LanguageCode)
            .Where(item => !languages.Contains(item))
            .ToArray())
        {
            option.RemoveTranslation(language);
        }

        option.SetActive(command.IsActive);
    }

    private static AdminAttributeDetail ToAttributeDetail(
        AttributeDefinition item) =>
        new(
            item.Id,
            item.Code,
            item.DataType.ToString(),
            item.UnitDimension,
            item.IsActive,
            item.Translations
                .OrderBy(value => value.LanguageCode, StringComparer.Ordinal)
                .Select(value => new AdminAttributeTranslationInput(
                    value.LanguageCode,
                    value.Name))
                .ToArray(),
            item.Options
                .OrderBy(value => value.SortOrder)
                .ThenBy(value => value.Code, StringComparer.Ordinal)
                .Select(value => ToDetail(value))
                .ToArray());

    private static AdminAttributeOptionDetail ToDetail(
        AttributeOption item) =>
        new(
            item.Id,
            item.Code,
            item.SortOrder,
            item.IsActive,
            item.Translations
                .OrderBy(value => value.LanguageCode)
                .Select(value => new AdminAttributeTranslationInput(
                    value.LanguageCode,
                    value.Name))
                .ToArray());

    private static AdminCategoryAttributeDetail ToDetail(
        CategoryAttributeAssignment item) =>
        new(
            item.CategoryId,
            item.AttributeId,
            item.IsRequired,
            item.IsFilterable,
            item.IsVisibleOnProduct,
            item.IsVisibleOnComparison,
            item.SortOrder);
}
