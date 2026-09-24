namespace Ekiphan.Domain.Catalog;

public sealed class ProductVariantSelection
{
    private ProductVariantSelection()
    {
    }

    internal ProductVariantSelection(
        Guid productVariantId,
        Guid variantGroupId,
        Guid variantOptionId)
    {
        ProductVariantId = productVariantId;
        VariantGroupId = variantGroupId;
        VariantOptionId = variantOptionId;
    }

    public Guid ProductVariantId { get; private set; }

    public Guid VariantGroupId { get; private set; }

    public Guid VariantOptionId { get; private set; }

    internal void SetOption(Guid variantOptionId)
    {
        if (variantOptionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Variant option identifier cannot be empty.",
                nameof(variantOptionId));
        }

        VariantOptionId = variantOptionId;
    }
}
