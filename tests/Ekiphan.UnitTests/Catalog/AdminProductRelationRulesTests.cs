using Ekiphan.Application.Catalog;
using Ekiphan.Domain.Catalog;

namespace Ekiphan.UnitTests.Catalog;

public sealed class AdminProductRelationRulesTests
{
    [Fact]
    public void ActiveDuplicateIsRejected()
    {
        var relation = Relation();

        var result = AdminProductRelationRules.ResolveExisting(relation);

        Assert.Equal(
            ExistingProductRelationResolution.RejectActive,
            result);
    }

    [Fact]
    public void InactiveManualRelationIsReactivated()
    {
        var relation = Relation();
        relation.SetActive(false);

        var result = AdminProductRelationRules.ResolveExisting(relation);

        Assert.Equal(
            ExistingProductRelationResolution.Reactivate,
            result);
    }

    [Fact]
    public void InactiveAutomaticRelationCannotBeTakenOver()
    {
        var relation = Relation(ProductRelationOrigin.Automatic);
        relation.SetActive(false);

        var result = AdminProductRelationRules.ResolveExisting(relation);

        Assert.Equal(
            ExistingProductRelationResolution.RejectAutomatic,
            result);
    }

    [Fact]
    public void BidirectionalRelationIsVisibleFromReverseDirection()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var relation = new ProductRelation(
            Guid.NewGuid(),
            sourceId,
            targetId,
            ProductRelationType.Similar,
            isBidirectional: true);
        var predicate = AdminProductRelationRules
            .VisibleManualRelationFor(targetId)
            .Compile();

        Assert.True(predicate(relation));
    }

    [Fact]
    public void UnidirectionalRelationIsNotVisibleFromReverseDirection()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var relation = new ProductRelation(
            Guid.NewGuid(),
            sourceId,
            targetId,
            ProductRelationType.Similar,
            isBidirectional: false);
        var predicate = AdminProductRelationRules
            .VisibleManualRelationFor(targetId)
            .Compile();

        Assert.False(predicate(relation));
    }

    [Fact]
    public void DeletedAndUntranslatedProductsAreNotSelectable()
    {
        var visible = Product("VISIBLE", addEnglish: true);
        var deleted = Product("DELETED", addEnglish: true);
        deleted.SoftDelete(DateTimeOffset.UtcNow);
        var untranslated = Product("TR-ONLY", addEnglish: false);
        var predicate = AdminProductRelationRules
            .SelectableProduct("en")
            .Compile();

        Assert.True(predicate(visible));
        Assert.False(predicate(deleted));
        Assert.False(predicate(untranslated));
    }

    [Fact]
    public void ActiveManualReverseRelationConflictsWhenEitherSideIsBidirectional()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var reverse = new ProductRelation(
            Guid.NewGuid(),
            targetId,
            sourceId,
            ProductRelationType.Complementary,
            isBidirectional: false);
        var command = new CreateAdminProductRelationCommand(
            sourceId,
            targetId,
            ProductRelationType.Complementary,
            IsBidirectional: true,
            SortOrder: 0);
        var predicate = AdminProductRelationRules
            .ConflictingReverse(command)
            .Compile();

        Assert.True(predicate(reverse));
    }

    private static ProductRelation Relation(
        ProductRelationOrigin origin = ProductRelationOrigin.Manual) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ProductRelationType.Similar,
            isBidirectional: false,
            origin: origin);

    private static Product Product(string sku, bool addEnglish)
    {
        var product = new Product(Guid.NewGuid(), sku);
        product.AddTranslation("tr", $"{sku} Türkçe", $"{sku.ToLowerInvariant()}-tr");
        if (addEnglish)
        {
            product.AddTranslation(
                "en",
                $"{sku} English",
                $"{sku.ToLowerInvariant()}-en");
        }

        return product;
    }
}
