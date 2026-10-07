using Ekiphan.Application.DataImport;
using Ekiphan.Domain.Catalog;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Ekiphan.Infrastructure.DataImport;

internal sealed class ImportReferenceResolver(EkiphanDbContext dbContext)
    : IImportReferenceResolver
{
    private const int QueryBatchSize = 1_000;

    public async Task<ImportReferenceResolution> ResolveAsync(
        IReadOnlyCollection<string> brandNames,
        IReadOnlyCollection<IReadOnlyList<string>> categoryPaths,
        IReadOnlyCollection<string> materialNames,
        IReadOnlyCollection<string> tagNames,
        CancellationToken cancellationToken = default)
    {
        var brandIds = new Dictionary<string, Guid>(
            StringComparer.OrdinalIgnoreCase);
        var existingBrands = await dbContext.Brands.ToListAsync(cancellationToken);
        var brandsByNormalizedName = existingBrands.GroupBy(brand => NormalizeName(brand.Name))
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        foreach (var brandName in brandNames.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            var key = NormalizeName(brandName);
            if (!brandsByNormalizedName.TryGetValue(key, out var brand))
            {
                brand = new Brand(Guid.NewGuid(), brandName.Trim());
                dbContext.Brands.Add(brand);
                brandsByNormalizedName[key] = brand;
            }
            brandIds[brandName] = brand.Id;
        }

        var categoryIds = new Dictionary<string, Guid>(
            StringComparer.OrdinalIgnoreCase);
        var ambiguousNames = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);
        var categoryPathIds = new Dictionary<string, IReadOnlyList<Guid>>(StringComparer.OrdinalIgnoreCase);
        var ambiguousPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var categories = await dbContext.Categories.Include(category => category.Translations)
            .ToListAsync(cancellationToken);
        var nodes = categories.Select(category => new
        {
            category.Id, category.ParentId, category.IsPublished, category.CreatedAt,
            Name = category.Translations.FirstOrDefault(translation => translation.LanguageCode == "tr")?.Name
        }).Where(node => !string.IsNullOrWhiteSpace(node.Name)).ToArray();
        var nodesById = nodes.ToDictionary(node => node.Id);
        foreach (var group in nodes.GroupBy(node => NormalizeName(node.Name!), StringComparer.Ordinal))
        {
            var identifiers = group.Select(match => match.Id).Distinct().ToArray();
            if (identifiers.Length == 1)
            {
                foreach (var name in group.Select(item => item.Name!).Distinct(StringComparer.OrdinalIgnoreCase))
                    categoryIds[name] = identifiers[0];
            }
            else
            {
                foreach (var name in group.Select(item => item.Name!).Distinct(StringComparer.OrdinalIgnoreCase))
                    ambiguousNames.Add(name);
            }
        }
        foreach (var path in categoryPaths)
        {
            var key = ImportCategoryPath.Key(path);
            var candidates = nodes.Where(node => NormalizeName(node.Name!) == NormalizeName(path[0]))
                .Select(node => (IReadOnlyList<Guid>)new[] { node.Id }).ToList();
            for (var index = 1; index < path.Count && candidates.Count > 0; index++)
            {
                var expected = NormalizeName(path[index]);
                candidates = candidates.SelectMany(candidate => nodes
                    .Where(node => node.ParentId == candidate[^1] && NormalizeName(node.Name!) == expected)
                    .Select(node => (IReadOnlyList<Guid>)candidate.Append(node.Id).ToArray())).ToList();
            }
            if (candidates.Count > 0)
            {
                categoryPathIds[key] = candidates
                    .OrderByDescending(candidate => nodesById[candidate[^1]].IsPublished)
                    .ThenByDescending(candidate => candidate.Count(id => nodesById[id].IsPublished))
                    .ThenBy(candidate => string.Join('|', candidate.Select(id => nodesById[id].CreatedAt.UtcTicks)))
                    .ThenBy(candidate => string.Join('|', candidate.Select(id => id.ToString("N"))), StringComparer.Ordinal)
                    .First();
                continue;
            }

            var section = await dbContext.ProductSections.OrderByDescending(item => item.IsPublished)
                .ThenBy(item => item.CreatedAt).ThenBy(item => item.Id).FirstOrDefaultAsync(cancellationToken);
            if (section is null)
            {
                continue;
            }

            var createdPath = new List<Guid>();
            Guid? parentId = null;
            foreach (var segment in path)
            {
                var matches = categories.Where(category => category.ParentId == parentId &&
                        category.Translations.Any(translation => translation.LanguageCode == "tr" &&
                            NormalizeName(translation.Name) == NormalizeName(segment)))
                    .OrderByDescending(category => category.IsPublished).ThenBy(category => category.CreatedAt)
                    .ThenBy(category => category.Id).ToArray();
                var category = matches.FirstOrDefault();
                if (category is null)
                {
                    category = new Category(Guid.NewGuid(), section.Id, parentId, categories.Count);
                    category.AddTranslation("tr", segment.Trim(), $"{Slug(segment)}-{category.Id:N}");
                    category.SetPublished(true);
                    dbContext.Categories.Add(category);
                    categories.Add(category);
                }
                createdPath.Add(category.Id);
                parentId = category.Id;
            }
            categoryPathIds[key] = createdPath;
        }

        Guid? materialAttributeId = null;
        var materialOptionIds = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var ambiguousMaterialNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (materialNames.Count > 0)
        {
            var materialAttribute = await dbContext.Attributes.Include(attribute => attribute.Options)
                .ThenInclude(option => option.Translations).Include(attribute => attribute.Translations)
                .SingleOrDefaultAsync(attribute => attribute.Code == "MATERIAL", cancellationToken);
            if (materialAttribute is null)
            {
                materialAttribute = new AttributeDefinition(Guid.NewGuid(), "MATERIAL", AttributeDataType.Option);
                materialAttribute.AddTranslation("tr", "Materyal");
                dbContext.Attributes.Add(materialAttribute);
            }
            materialAttribute.SetActive(true);
            materialAttributeId = materialAttribute.Id;
            foreach (var materialName in materialNames.Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                var normalized = NormalizeName(materialName);
                var option = materialAttribute.Options.FirstOrDefault(item => item.Translations.Any(translation =>
                    translation.LanguageCode == "tr" && NormalizeName(translation.Name) == normalized));
                if (option is null)
                {
                    var code = "MATERIAL_" + SlugCode(materialName);
                    option = materialAttribute.Options.FirstOrDefault(item => item.Code == code) ??
                        materialAttribute.AddOption(Guid.NewGuid(), code, materialAttribute.Options.Count);
                    if (!option.Translations.Any(item => item.LanguageCode == "tr")) option.AddTranslation("tr", materialName.Trim());
                }
                option.SetActive(true);
                materialOptionIds[materialName] = option.Id;
            }
        }

        var allTags = await dbContext.Tags.Include(tag => tag.Translations).ToListAsync(cancellationToken);
        var tagMatches = new List<(string Name, Guid Id)>();
        foreach (var tagName in tagNames.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            var normalized = NormalizeName(tagName);
            var matches = allTags.Where(tag => tag.Translations.Any(translation => translation.LanguageCode == "tr" &&
                NormalizeName(translation.Name) == normalized)).OrderByDescending(tag => tag.IsActive)
                .ThenBy(tag => tag.CreatedAt).ThenBy(tag => tag.Id).ToArray();
            var tag = matches.FirstOrDefault();
            if (tag is null)
            {
                tag = new Tag(Guid.NewGuid(), $"TAG_{SlugCode(tagName)}_{Guid.NewGuid():N}"[..Math.Min(100, 37 + SlugCode(tagName).Length)]);
                tag.AddTranslation("tr", tagName.Trim(), $"{Slug(tagName)}-{tag.Id:N}");
                dbContext.Tags.Add(tag);
                allTags.Add(tag);
            }
            tag.SetActive(true);
            tagMatches.Add((tagName, tag.Id));
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
            ambiguousTagNames)
        {
            CategoryPathIds = categoryPathIds,
            AmbiguousCategoryPaths = ambiguousPaths
        };
    }

    internal static string NormalizeName(string value)
    {
        var decomposed = value.Trim().Replace('İ', 'I').Replace('ı', 'i').ToUpperInvariant()
            .Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.IsLetterOrDigit(character) ? character : ' ');
        return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
    }

    private static string SlugCode(string value) => NormalizeName(value).Replace(' ', '_');

    private static string Slug(string value) => NormalizeName(value).ToLowerInvariant().Replace(' ', '-');
}
