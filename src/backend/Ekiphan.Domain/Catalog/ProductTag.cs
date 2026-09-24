namespace Ekiphan.Domain.Catalog;

public sealed class ProductTag
{
    private ProductTag()
    {
    }

    internal ProductTag(Guid productId, Guid tagId, int sortOrder)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product identifier cannot be empty.",
                nameof(productId));
        }

        if (tagId == Guid.Empty)
        {
            throw new ArgumentException(
                "Tag identifier cannot be empty.",
                nameof(tagId));
        }

        ProductId = productId;
        TagId = tagId;
        SortOrder = sortOrder;
    }

    public Guid ProductId { get; private set; }

    public Guid TagId { get; private set; }

    public int SortOrder { get; private set; }

    internal void SetSortOrder(int sortOrder) => SortOrder = sortOrder;
}
