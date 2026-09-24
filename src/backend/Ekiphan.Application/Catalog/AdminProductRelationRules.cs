using System.Linq.Expressions;
using Ekiphan.Domain.Catalog;

namespace Ekiphan.Application.Catalog;

public enum ExistingProductRelationResolution
{
    Create = 1,
    Reactivate = 2,
    RejectActive = 3,
    RejectAutomatic = 4,
}

public static class AdminProductRelationRules
{
    public static ExistingProductRelationResolution ResolveExisting(
        ProductRelation? relation)
    {
        if (relation is null)
        {
            return ExistingProductRelationResolution.Create;
        }

        if (relation.IsActive)
        {
            return ExistingProductRelationResolution.RejectActive;
        }

        return relation.Origin == ProductRelationOrigin.Manual
            ? ExistingProductRelationResolution.Reactivate
            : ExistingProductRelationResolution.RejectAutomatic;
    }

    public static Expression<Func<Product, bool>> SelectableProduct(
        string languageCode) =>
        product =>
            !product.IsDeleted &&
            product.Translations.Any(translation =>
                translation.LanguageCode == languageCode);

    public static Expression<Func<ProductRelation, bool>>
        VisibleManualRelationFor(Guid productId) =>
            relation =>
                relation.IsActive &&
                relation.Origin == ProductRelationOrigin.Manual &&
                (relation.SourceProductId == productId ||
                 relation.IsBidirectional &&
                 relation.TargetProductId == productId);

    public static Expression<Func<ProductRelation, bool>>
        ConflictingReverse(CreateAdminProductRelationCommand command) =>
            relation =>
                relation.SourceProductId == command.TargetProductId &&
                relation.TargetProductId == command.SourceProductId &&
                relation.RelationType == command.RelationType &&
                relation.Origin == ProductRelationOrigin.Manual &&
                relation.IsActive &&
                (relation.IsBidirectional || command.IsBidirectional);
}
