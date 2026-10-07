using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Catalog;

public sealed class ProductVariant : Entity
{
    private readonly List<ProductVariantSelection> _selections = [];

    private ProductVariant()
    {
    }

    internal ProductVariant(
        Guid id,
        Guid productId,
        string sku,
        IReadOnlyDictionary<Guid, Guid> selectedOptions,
        int sortOrder,
        Guid? mediaAssetId = null)
        : base(id)
    {
        ProductId = productId;
        SKU = SkuNormalizer.Normalize(CatalogGuard.Required(sku, 100, nameof(sku)));
        SortOrder = sortOrder;
        MediaAssetId = mediaAssetId;

        foreach (var selection in selectedOptions.OrderBy(item => item.Key))
        {
            _selections.Add(
                new ProductVariantSelection(id, selection.Key, selection.Value));
        }
    }

    public Guid ProductId { get; private set; }

    public string SKU { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    public int SortOrder { get; private set; }

    public Guid? MediaAssetId { get; private set; }

    public IReadOnlyCollection<ProductVariantSelection> Selections => _selections;

    public void SetActive(bool isActive) => IsActive = isActive;

    internal void Update(
        string sku,
        IReadOnlyDictionary<Guid, Guid> selectedOptions,
        int sortOrder,
        Guid? mediaAssetId = null)
    {
        SKU = SkuNormalizer.Normalize(CatalogGuard.Required(sku, 100, nameof(sku)));
        SortOrder = sortOrder;
        MediaAssetId = mediaAssetId;
        foreach (var selection in _selections)
        {
            selection.SetOption(selectedOptions[selection.VariantGroupId]);
        }
    }

    internal bool HasSameSelection(IReadOnlyDictionary<Guid, Guid> selectedOptions) =>
        _selections.Count == selectedOptions.Count &&
        _selections.All(
            selection =>
                selectedOptions.TryGetValue(selection.VariantGroupId, out var optionId) &&
                optionId == selection.VariantOptionId);
}
