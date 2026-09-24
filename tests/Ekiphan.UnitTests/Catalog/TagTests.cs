using Ekiphan.Domain.Catalog;

namespace Ekiphan.UnitTests.Catalog;

public sealed class TagTests
{
    [Fact]
    public void CodeAndTranslationLanguagesAreNormalized()
    {
        var tag = new Tag(Guid.NewGuid(), " premium ");

        tag.AddTranslation("TR", "Premium", "premium");

        Assert.Equal("PREMIUM", tag.Code);
        Assert.Equal("tr", Assert.Single(tag.Translations).LanguageCode);
    }

    [Fact]
    public void DuplicateTranslationLanguageIsRejected()
    {
        var tag = new Tag(Guid.NewGuid(), "HOTEL");
        tag.AddTranslation("tr", "Otel", "otel");

        Assert.Throws<InvalidOperationException>(
            () => tag.AddTranslation("TR", "Konaklama", "konaklama"));
    }

    [Fact]
    public void ExistingTranslationCanBeUpdated()
    {
        var tag = new Tag(Guid.NewGuid(), "HOTEL");
        tag.AddTranslation("tr", "Otel", "otel");

        tag.SetTranslation("TR", "Konaklama", "konaklama");

        var translation = Assert.Single(tag.Translations);
        Assert.Equal("Konaklama", translation.Name);
        Assert.Equal("konaklama", translation.Slug);
    }

    [Fact]
    public void ProductRejectsDuplicateTag()
    {
        var product = new Product(Guid.NewGuid(), "TAG-1");
        var tagId = Guid.NewGuid();
        product.AddTag(tagId);

        Assert.Throws<InvalidOperationException>(() => product.AddTag(tagId));
    }
}
