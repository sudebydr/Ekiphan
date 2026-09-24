using Ekiphan.Application.DataImport;
using Ekiphan.Domain.Catalog;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.DataImport;

internal sealed class ImportReferenceResolver(EkiphanDbContext dbContext)
    : IImportReferenceResolver
{
    private const int QueryBatchSize = 1_000;

    public async Task<ImportReferenceResolution> ResolveAsync(
        IReadOnlyCollection<string> brandNames,
        IReadOnlyCollection<string> categoryNames,
        IReadOnlyCollection<string> materialNames,
        IReadOnlyCollection<string> tagNames,
        CancellationToken cancellationToken = default)
    {
        var brandIds = new Dictionary<string, Guid>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var batch in brandNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Chunk(QueryBatchSize))
        {
            var matches = await dbContext.Brands
                .AsNoTracking()
                .Where(brand => batch.Contains(brand.Name))
                .Select(brand => new
                {
                    brand.Name,
                    brand.Id,
                })
                .ToListAsync(cancellationToken);
            foreach (var match in matches)
            {
                brandIds[match.Name] = match.Id;
            }
        }

        var categoryMatches = new List<(string Name, Guid Id)>();
        foreach (var batch in categoryNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Chunk(QueryBatchSize))
        {
            var matches = await dbContext.Set<CategoryTranslation>()
                .AsNoTracking()
                .Where(
                    translation =>
                        translation.LanguageCode == "tr" &&
                        batch.Contains(translation.Name))
                .Select(translation => new
                {
                    translation.Name,
                    Id = translation.CategoryId,
                })
                .ToListAsync(cancellationToken);
            categoryMatches.AddRange(
                matches.Select(match => (match.Name, match.Id)));
        }

        var categoryIds = new Dictionary<string, Guid>(
            StringComparer.OrdinalIgnoreCase);
        var ambiguousNames = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var group in categoryMatches.GroupBy(
            match => match.Name,
            StringComparer.OrdinalIgnoreCase))
        {
            var identifiers = group.Select(match => match.Id).Distinct().ToArray();
            if (identifiers.Length == 1)
            {
                categoryIds[group.Key] = identifiers[0];
            }
            else
            {
                ambiguousNames.Add(group.Key);
            }
        }

        Guid? materialAttributeId = null;
        if (materialNames.Count > 0)
        {
            materialAttributeId = await dbContext.Attributes
                .AsNoTracking()
                .Where(
                    attribute =>
                        attribute.Code == "MATERIAL" &&
                        attribute.IsActive &&
                        (attribute.DataType == AttributeDataType.Option ||
                         attribute.DataType == AttributeDataType.MultiOption))
                .Select(attribute => (Guid?)attribute.Id)
                .SingleOrDefaultAsync(cancellationToken);
        }
        var materialOptionIds = new Dictionary<string, Guid>(
            StringComparer.OrdinalIgnoreCase);
        var ambiguousMaterialNames = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);
        if (materialAttributeId.HasValue)
        {
            var materialMatches = new List<(string Name, Guid Id)>();
            foreach (var batch in materialNames
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Chunk(QueryBatchSize))
            {
                var matches = await dbContext.Set<AttributeOptionTranslation>()
                    .AsNoTracking()
                    .Join(
                        dbContext.Set<AttributeOption>()
                            .Where(
                                option =>
                                    option.AttributeId == materialAttributeId &&
                                    option.IsActive),
                        translation => translation.AttributeOptionId,
                        option => option.Id,
                        (translation, option) => translation)
                    .Where(
                        translation =>
                            translation.LanguageCode == "tr" &&
                            batch.Contains(translation.Name))
                    .Select(translation => new
                    {
                        translation.Name,
                        Id = translation.AttributeOptionId,
                    })
                    .ToListAsync(cancellationToken);
                materialMatches.AddRange(
                    matches.Select(match => (match.Name, match.Id)));
            }

            foreach (var group in materialMatches.GroupBy(
                match => match.Name,
                StringComparer.OrdinalIgnoreCase))
            {
                var identifiers = group
                    .Select(match => match.Id)
                    .Distinct()
                    .ToArray();
                if (identifiers.Length == 1)
                {
                    materialOptionIds[group.Key] = identifiers[0];
                }
                else
                {
                    ambiguousMaterialNames.Add(group.Key);
                }
            }
        }

        var tagMatches = new List<(string Name, Guid Id)>();
        foreach (var batch in tagNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Chunk(QueryBatchSize))
        {
            var matches = await dbContext.Set<TagTranslation>()
                .AsNoTracking()
                .Join(
                    dbContext.Tags.Where(tag => tag.IsActive),
                    translation => translation.TagId,
                    tag => tag.Id,
                    (translation, tag) => translation)
                .Where(
                    translation =>
                        translation.LanguageCode == "tr" &&
                        batch.Contains(translation.Name))
                .Select(translation => new
                {
                    translation.Name,
                    Id = translation.TagId,
                })
                .ToListAsync(cancellationToken);
            tagMatches.AddRange(matches.Select(match => (match.Name, match.Id)));
        }

        var tagIds = new Dictionary<string, Guid>(
            StringComparer.OrdinalIgnoreCase);
        var ambiguousTagNames = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var group in tagMatches.GroupBy(
            match => match.Name,
            StringComparer.OrdinalIgnoreCase))
        {
            var identifiers = group.Select(match => match.Id).Distinct().ToArray();
            if (identifiers.Length == 1)
            {
                tagIds[group.Key] = identifiers[0];
            }
            else
            {
                ambiguousTagNames.Add(group.Key);
            }
        }

        return new ImportReferenceResolution(
            brandIds,
            categoryIds,
            ambiguousNames,
            materialAttributeId,
            materialOptionIds,
            ambiguousMaterialNames,
            tagIds,
            ambiguousTagNames);
    }
}
