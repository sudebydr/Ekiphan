namespace Ekiphan.Domain.Catalog;

public sealed class CategoryAttributeAssignment
{
    private CategoryAttributeAssignment()
    {
    }

    public CategoryAttributeAssignment(
        Guid categoryId,
        Guid attributeId,
        bool isRequired = false,
        bool isFilterable = false,
        bool isVisibleOnProduct = true,
        bool isVisibleOnComparison = false,
        int sortOrder = 0)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException(
                "Category identifier cannot be empty.",
                nameof(categoryId));
        }

        if (attributeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Attribute identifier cannot be empty.",
                nameof(attributeId));
        }

        CategoryId = categoryId;
        AttributeId = attributeId;
        IsRequired = isRequired;
        IsFilterable = isFilterable;
        IsVisibleOnProduct = isVisibleOnProduct;
        IsVisibleOnComparison = isVisibleOnComparison;
        SortOrder = sortOrder;
    }

    public Guid CategoryId { get; private set; }

    public Guid AttributeId { get; private set; }

    public bool IsRequired { get; private set; }

    public bool IsFilterable { get; private set; }

    public bool IsVisibleOnProduct { get; private set; }

    public bool IsVisibleOnComparison { get; private set; }

    public int SortOrder { get; private set; }

    public void Update(
        bool isRequired,
        bool isFilterable,
        bool isVisibleOnProduct,
        bool isVisibleOnComparison,
        int sortOrder)
    {
        IsRequired = isRequired;
        IsFilterable = isFilterable;
        IsVisibleOnProduct = isVisibleOnProduct;
        IsVisibleOnComparison = isVisibleOnComparison;
        SortOrder = sortOrder;
    }
}
