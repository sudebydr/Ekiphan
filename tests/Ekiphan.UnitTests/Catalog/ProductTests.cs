using Ekiphan.Domain.Catalog;

namespace Ekiphan.UnitTests.Catalog;

public sealed class ProductTests
{
    [Fact]
    public void ConstructorNormalizesSku()
    {
        var product = new Product(Guid.NewGuid(), " 3532.rub.ex.pt500 ");

        Assert.Equal("3532.RUB.EX.PT500", product.SKU);
    }

    [Fact]
    public void SettingPrimaryCategoryClearsPreviousPrimaryCategory()
    {
        var firstCategoryId = Guid.NewGuid();
        var secondCategoryId = Guid.NewGuid();
        var product = new Product(Guid.NewGuid(), "SKU-1");

        product.AddCategory(firstCategoryId, isPrimary: true);
        product.AddCategory(secondCategoryId);
        product.SetPrimaryCategory(secondCategoryId);

        Assert.Equal(secondCategoryId, product.PrimaryCategoryId);
        Assert.False(
            product.Categories.Single(item => item.CategoryId == firstCategoryId).IsPrimary);
        Assert.True(
            product.Categories.Single(item => item.CategoryId == secondCategoryId).IsPrimary);
    }

    [Fact]
    public void PrimaryCategoryMustBeAssignedToProduct()
    {
        var product = new Product(Guid.NewGuid(), "SKU-1");

        var exception = Assert.Throws<InvalidOperationException>(
            () => product.SetPrimaryCategory(Guid.NewGuid()));

        Assert.Equal(
            "Primary category must already be assigned to the product.",
            exception.Message);
    }

    [Fact]
    public void SetCategoriesSynchronizesAssignmentsAndPrimaryCategory()
    {
        var removedId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var primaryId = Guid.NewGuid();
        var product = new Product(Guid.NewGuid(), "SKU-1");
        product.AddCategory(removedId, isPrimary: true);

        product.SetCategories([firstId, primaryId], primaryId);

        Assert.Equal(primaryId, product.PrimaryCategoryId);
        Assert.DoesNotContain(
            product.Categories,
            item => item.CategoryId == removedId);
        Assert.Equal(2, product.Categories.Count);
        Assert.True(
            product.Categories.Single(
                item => item.CategoryId == primaryId).IsPrimary);
    }

    [Fact]
    public void SetCategoriesRejectsPrimaryOutsideAssignments()
    {
        var product = new Product(Guid.NewGuid(), "SKU-1");

        Assert.Throws<ArgumentException>(
            () => product.SetCategories([], Guid.NewGuid()));
    }

    [Fact]
    public void SetTagsSynchronizesAssignmentsAndOrder()
    {
        var removedId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var product = new Product(Guid.NewGuid(), "SKU-1");
        product.AddTag(removedId);
        product.AddTag(secondId);

        product.SetTags([firstId, secondId]);

        Assert.DoesNotContain(product.Tags, item => item.TagId == removedId);
        Assert.Equal(
            [firstId, secondId],
            product.Tags
                .OrderBy(item => item.SortOrder)
                .Select(item => item.TagId)
                .ToArray());
    }

    [Fact]
    public void DuplicateTranslationLanguageIsRejected()
    {
        var product = new Product(Guid.NewGuid(), "SKU-1");
        product.AddTranslation("tr", "Ürün", "urun");

        Assert.Throws<InvalidOperationException>(
            () => product.AddTranslation("TR", "Başka Ürün", "baska-urun"));
    }

    [Fact]
    public void IdentityAndExistingTranslationCanBeUpdated()
    {
        var brandId = Guid.NewGuid();
        var product = new Product(Guid.NewGuid(), "OLD");
        product.AddTranslation("tr", "Eski ad", "eski-ad");

        product.UpdateIdentity(" new-sku ", brandId);
        product.SetTranslation(
            "TR",
            "Yeni ad",
            "yeni-ad",
            "Kısa açıklama",
            "Uzun açıklama");

        var translation = Assert.Single(product.Translations);
        Assert.Equal("NEW-SKU", product.SKU);
        Assert.Equal(brandId, product.BrandId);
        Assert.Equal("Yeni ad", translation.Name);
        Assert.Equal("yeni-ad", translation.Slug);
    }

    [Fact]
    public void ProductCannotRemoveItsOnlyTranslation()
    {
        var product = new Product(Guid.NewGuid(), "SKU-1");
        product.AddTranslation("tr", "Ürün", "urun");

        Assert.Throws<InvalidOperationException>(
            () => product.RemoveTranslation("tr"));
    }

    [Fact]
    public void SoftDeleteAlsoUnpublishesProduct()
    {
        var product = new Product(Guid.NewGuid(), "SKU-1");
        product.SetPublished(true);
        var deletedAt = DateTimeOffset.UtcNow;

        product.SoftDelete(deletedAt);

        Assert.True(product.IsDeleted);
        Assert.False(product.IsPublished);
        Assert.Equal(deletedAt, product.DeletedAt);
    }
}
