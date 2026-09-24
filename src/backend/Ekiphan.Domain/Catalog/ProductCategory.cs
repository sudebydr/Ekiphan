namespace Ekiphan.Domain.Catalog;

public sealed class ProductCategory
{
    private ProductCategory()
    {
    }

    internal ProductCategory(
        Guid productId,
        Guid categoryId,
        bool isPrimary,
        int sortOrder)
    {
        ProductId = productId;
        CategoryId = categoryId;
        IsPrimary = isPrimary;
        SortOrder = sortOrder;
    }

    public Guid ProductId { get; private set; }

    public Guid CategoryId { get; private set; }

    public bool IsPrimary { get; private set; }

    public int SortOrder { get; private set; }

    internal void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;

    public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;
}
