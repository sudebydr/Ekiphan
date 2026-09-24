using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Catalog;

public sealed class ProductRelation : Entity
{
    private ProductRelation()
    {
    }

    public ProductRelation(
        Guid id,
        Guid sourceProductId,
        Guid targetProductId,
        ProductRelationType relationType,
        bool isBidirectional,
        int sortOrder = 0,
        ProductRelationOrigin origin = ProductRelationOrigin.Manual)
        : base(id)
    {
        if (sourceProductId == Guid.Empty)
        {
            throw new ArgumentException(
                "Source product identifier cannot be empty.",
                nameof(sourceProductId));
        }

        if (targetProductId == Guid.Empty)
        {
            throw new ArgumentException(
                "Target product identifier cannot be empty.",
                nameof(targetProductId));
        }

        if (sourceProductId == targetProductId)
        {
            throw new ArgumentException(
                "A product cannot be related to itself.",
                nameof(targetProductId));
        }

        if (!Enum.IsDefined(relationType))
        {
            throw new ArgumentOutOfRangeException(nameof(relationType));
        }

        if (!Enum.IsDefined(origin))
        {
            throw new ArgumentOutOfRangeException(nameof(origin));
        }

        SourceProductId = sourceProductId;
        TargetProductId = targetProductId;
        RelationType = relationType;
        IsBidirectional = isBidirectional;
        SortOrder = sortOrder;
        Origin = origin;
    }

    public Guid SourceProductId { get; private set; }

    public Guid TargetProductId { get; private set; }

    public ProductRelationType RelationType { get; private set; }

    public ProductRelationOrigin Origin { get; private set; }

    public bool IsBidirectional { get; private set; }

    public bool IsActive { get; private set; } = true;

    public int SortOrder { get; private set; }

    public void SetActive(bool isActive) => IsActive = isActive;

    public void Configure(bool isBidirectional, int sortOrder)
    {
        IsBidirectional = isBidirectional;
        SortOrder = sortOrder;
        IsActive = true;
    }
}
