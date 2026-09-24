using Ekiphan.Domain.Catalog;

namespace Ekiphan.UnitTests.Catalog;

public sealed class CategoryTests
{
    [Fact]
    public void UpdateRejectsSelfAsParent()
    {
        var id = Guid.NewGuid();
        var category = new Category(id, Guid.NewGuid());

        Assert.Throws<ArgumentException>(
            () => category.Update(Guid.NewGuid(), id, 0));
    }

    [Fact]
    public void SetTranslationUpdatesExistingValue()
    {
        var category = new Category(Guid.NewGuid(), Guid.NewGuid());
        category.AddTranslation("tr", "Eski", "eski");

        category.SetTranslation("TR", "Yeni", "yeni", "Açıklama");

        var translation = Assert.Single(category.Translations);
        Assert.Equal("Yeni", translation.Name);
        Assert.Equal("yeni", translation.Slug);
        Assert.Equal("Açıklama", translation.Description);
    }

    [Fact]
    public void CategoryCannotBeItsOwnParent()
    {
        var categoryId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(
            () => new Category(categoryId, Guid.NewGuid(), categoryId));
    }

    [Fact]
    public void UnsupportedTranslationLanguageIsRejected()
    {
        var category = new Category(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<ArgumentOutOfRangeException>(
            () => category.AddTranslation("de", "Gläser", "glaeser"));
    }
}
