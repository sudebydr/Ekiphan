using Ekiphan.Application.Catalog;
using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.Media;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Catalog;

internal sealed class AdminVariantService(EkiphanDbContext dbContext)
    : IAdminVariantService
{
    public async Task<AdminProductVariantCatalog?> GetAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Include(item => item.VariantGroups)
                .ThenInclude(item => item.Translations)
            .Include(item => item.VariantGroups)
                .ThenInclude(item => item.Options)
                    .ThenInclude(item => item.Translations)
            .Include(item => item.Variants)
                .ThenInclude(item => item.Selections)
            .SingleOrDefaultAsync(item => item.Id == productId, cancellationToken);
        return product is null ? null : ToDetail(product);
    }

    public async Task<AdminVariantGroupDetail> CreateGroupAsync(
        Guid productId,
        SaveVariantGroupCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command.Translations);
        var product = await LoadAsync(productId, cancellationToken)
            ?? throw new ArgumentException("The selected product does not exist.");
        var group = product.AddVariantGroup(
            Guid.NewGuid(),
            command.Code,
            command.SortOrder);
        Apply(group, command);
        await SaveAsync(cancellationToken);
        return (await RequiredAsync(productId, cancellationToken))
            .Groups.Single(item => item.Id == group.Id);
    }

    public async Task<AdminVariantGroupDetail?> UpdateGroupAsync(
        Guid productId,
        Guid groupId,
        SaveVariantGroupCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command.Translations);
        var product = await LoadAsync(productId, cancellationToken);
        var group = product?.VariantGroups.SingleOrDefault(item => item.Id == groupId);
        if (group is null)
        {
            return null;
        }

        if (product!.VariantGroups.Any(item =>
                item.Id != groupId &&
                string.Equals(
                    item.Code,
                    command.Code.Trim(),
                    StringComparison.OrdinalIgnoreCase)))
        {
            throw new AdminVariantConflictException(
                "The variant group code is already in use.");
        }

        group.Update(command.Code, command.SortOrder);
        Apply(group, command);
        await SaveAsync(cancellationToken);
        return (await RequiredAsync(productId, cancellationToken))
            .Groups.Single(item => item.Id == groupId);
    }

    public async Task<AdminVariantOptionDetail> CreateOptionAsync(
        Guid productId,
        Guid groupId,
        SaveVariantOptionCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command.Translations);
        var product = await LoadAsync(productId, cancellationToken)
            ?? throw new ArgumentException("The selected product does not exist.");
        var group = product.VariantGroups.SingleOrDefault(item => item.Id == groupId)
            ?? throw new ArgumentException("The selected variant group does not exist.");
        var option = group.AddOption(
            Guid.NewGuid(),
            command.Code,
            command.SortOrder);
        Apply(option, command);
        await SaveAsync(cancellationToken);
        return (await RequiredAsync(productId, cancellationToken))
            .Groups.SelectMany(item => item.Options)
            .Single(item => item.Id == option.Id);
    }

    public async Task<AdminVariantOptionDetail?> UpdateOptionAsync(
        Guid productId,
        Guid optionId,
        SaveVariantOptionCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command.Translations);
        var product = await LoadAsync(productId, cancellationToken);
        var group = product?.VariantGroups.SingleOrDefault(
            item => item.Options.Any(option => option.Id == optionId));
        var option = group?.Options.Single(item => item.Id == optionId);
        if (option is null)
        {
            return null;
        }

        if (group!.Options.Any(item =>
                item.Id != optionId &&
                string.Equals(
                    item.Code,
                    command.Code.Trim(),
                    StringComparison.OrdinalIgnoreCase)))
        {
            throw new AdminVariantConflictException(
                "The variant option code is already in use.");
        }

        option.Update(command.Code, command.SortOrder);
        Apply(option, command);
        await SaveAsync(cancellationToken);
        return (await RequiredAsync(productId, cancellationToken))
            .Groups.SelectMany(item => item.Options)
            .Single(item => item.Id == optionId);
    }

    public async Task<AdminProductVariantDetail> CreateVariantAsync(
        Guid productId,
        SaveProductVariantCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        await EnsureImageExistsAsync(command.MediaAssetId, cancellationToken);
        var product = await LoadAsync(productId, cancellationToken)
            ?? throw new ArgumentException("The selected product does not exist.");
        var variant = product.AddVariant(
            Guid.NewGuid(),
            command.SKU,
            command.SelectedOptions,
            command.SortOrder,
            command.MediaAssetId);
        variant.SetActive(command.IsActive);
        await SaveAsync(cancellationToken);
        return (await RequiredAsync(productId, cancellationToken))
            .Variants.Single(item => item.Id == variant.Id);
    }

    public async Task<AdminProductVariantDetail?> UpdateVariantAsync(
        Guid productId,
        Guid variantId,
        SaveProductVariantCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        await EnsureImageExistsAsync(command.MediaAssetId, cancellationToken);
        var product = await LoadAsync(productId, cancellationToken);
        if (product is null ||
            !product.Variants.Any(item => item.Id == variantId))
        {
            return null;
        }

        product.UpdateVariant(
            variantId,
            command.SKU,
            command.SelectedOptions,
            command.SortOrder,
            command.IsActive,
            command.MediaAssetId);
        await SaveAsync(cancellationToken);
        return (await RequiredAsync(productId, cancellationToken))
            .Variants.Single(item => item.Id == variantId);
    }

    private Task<Product?> LoadAsync(
        Guid productId,
        CancellationToken cancellationToken) =>
        dbContext.Products
            .Include(item => item.VariantGroups)
                .ThenInclude(item => item.Translations)
            .Include(item => item.VariantGroups)
                .ThenInclude(item => item.Options)
                    .ThenInclude(item => item.Translations)
            .Include(item => item.Variants)
                .ThenInclude(item => item.Selections)
            .SingleOrDefaultAsync(item => item.Id == productId, cancellationToken);

    private static AdminProductVariantCatalog ToDetail(Product product) =>
            new(
                product.Id,
                product.SKU,
                product.VariantGroups
                    .OrderBy(group => group.SortOrder)
                    .Select(group => new AdminVariantGroupDetail(
                        group.Id,
                        group.Code,
                        group.SortOrder,
                        group.Translations
                            .OrderBy(text => text.LanguageCode)
                            .Select(text => new AdminVariantTranslationInput(
                                text.LanguageCode,
                                text.Name))
                            .ToArray(),
                        group.Options
                            .OrderBy(option => option.SortOrder)
                            .Select(option => new AdminVariantOptionDetail(
                                option.Id,
                                option.Code,
                                option.SortOrder,
                                option.IsActive,
                                option.Translations
                                    .OrderBy(text => text.LanguageCode)
                                    .Select(text => new AdminVariantTranslationInput(
                                        text.LanguageCode,
                                        text.Name))
                                    .ToArray()))
                            .ToArray()))
                    .ToArray(),
                product.Variants
                    .OrderBy(variant => variant.SortOrder)
                    .Select(variant => new AdminProductVariantDetail(
                        variant.Id,
                        variant.SKU,
                        variant.SortOrder,
                        variant.IsActive,
                        variant.Selections.ToDictionary(
                            selection => selection.VariantGroupId,
                            selection => selection.VariantOptionId),
                        variant.MediaAssetId))
                    .ToArray());

    private async Task<AdminProductVariantCatalog> RequiredAsync(
        Guid productId,
        CancellationToken cancellationToken) =>
        await GetAsync(productId, cancellationToken)
            ?? throw new InvalidOperationException("Saved variants could not be reloaded.");

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new AdminVariantConflictException(
                "The variant code, SKU, or combination is already in use.");
        }
    }

    private static void Validate(
        IReadOnlyList<AdminVariantTranslationInput>? translations)
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

    private static void Validate(SaveProductVariantCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.SelectedOptions is null ||
            command.SelectedOptions.Count is < 1 or > 2)
        {
            throw new ArgumentException(
                "A variant must select one or two options.");
        }
    }

    private async Task EnsureImageExistsAsync(
        Guid? mediaAssetId,
        CancellationToken cancellationToken)
    {
        if (!mediaAssetId.HasValue)
        {
            return;
        }

        if (!await dbContext.MediaAssets.AsNoTracking().AnyAsync(
                asset => asset.Id == mediaAssetId.Value &&
                    asset.AssetType == MediaAssetType.Image &&
                    asset.Status == MediaStatus.Active,
                cancellationToken))
        {
            throw new ArgumentException(
                "The selected variant image does not exist or is inactive.",
                nameof(mediaAssetId));
        }
    }

    private static void Apply(
        ProductVariantGroup group,
        SaveVariantGroupCommand command)
    {
        ApplyTranslations(
            group.Translations.Select(item => item.LanguageCode).ToArray(),
            command.Translations,
            group.SetTranslation,
            group.RemoveTranslation);
    }

    private static void Apply(
        ProductVariantOption option,
        SaveVariantOptionCommand command)
    {
        ApplyTranslations(
            option.Translations.Select(item => item.LanguageCode).ToArray(),
            command.Translations,
            option.SetTranslation,
            option.RemoveTranslation);
        option.SetActive(command.IsActive);
    }

    private static void ApplyTranslations(
        IReadOnlyList<string> existing,
        IReadOnlyList<AdminVariantTranslationInput> requested,
        Action<string, string> set,
        Action<string> remove)
    {
        var languages = requested
            .Select(item => item.LanguageCode.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
        foreach (var translation in requested)
        {
            set(translation.LanguageCode, translation.Name);
        }

        foreach (var language in existing.Where(item => !languages.Contains(item)))
        {
            remove(language);
        }
    }
}
