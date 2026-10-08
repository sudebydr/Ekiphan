using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Catalog;

public sealed class PendingProductRelation : Entity
{
    private PendingProductRelation() { }

    public PendingProductRelation(Guid sourceProductId, string targetSku, ProductRelationType relationType, int sortOrder)
        : base(Guid.NewGuid())
    {
        if (sourceProductId == Guid.Empty) throw new ArgumentException("Source product is required.", nameof(sourceProductId));
        SourceProductId = sourceProductId;
        TargetNormalizedSku = SkuNormalizer.Normalize(targetSku);
        RelationType = relationType;
        SortOrder = sortOrder;
    }

    public Guid SourceProductId { get; private set; }
    public string TargetNormalizedSku { get; private set; } = null!;
    public ProductRelationType RelationType { get; private set; }
    public int SortOrder { get; private set; }
}
