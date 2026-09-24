using Ekiphan.Domain.Catalog;

namespace Ekiphan.UnitTests.Catalog;

public sealed class ProductRelationTests
{
    [Fact]
    public void ProductCannotRelateToItself()
    {
        var productId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(
            () => new ProductRelation(
                Guid.NewGuid(),
                productId,
                productId,
                ProductRelationType.Similar,
                isBidirectional: true));
    }

    [Fact]
    public void DirectionAndOriginAreStoredExplicitly()
    {
        var relation = new ProductRelation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ProductRelationType.Complementary,
            isBidirectional: false,
            origin: ProductRelationOrigin.Manual);

        Assert.False(relation.IsBidirectional);
        Assert.Equal(ProductRelationOrigin.Manual, relation.Origin);
        Assert.Equal(ProductRelationType.Complementary, relation.RelationType);
    }

    [Fact]
    public void RelationCanBeDeactivatedWithoutDeletion()
    {
        var relation = new ProductRelation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ProductRelationType.Accessory,
            isBidirectional: false);

        relation.SetActive(false);

        Assert.False(relation.IsActive);
    }

    [Fact]
    public void InactiveRelationCanBeReconfiguredAndReactivated()
    {
        var relation = new ProductRelation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ProductRelationType.Similar,
            isBidirectional: false,
            sortOrder: 1);
        relation.SetActive(false);

        relation.Configure(isBidirectional: true, sortOrder: 8);

        Assert.True(relation.IsActive);
        Assert.True(relation.IsBidirectional);
        Assert.Equal(8, relation.SortOrder);
    }
}
