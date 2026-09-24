using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Catalog;

public sealed class Tag : Entity
{
    private readonly List<TagTranslation> _translations = [];

    private Tag()
    {
    }

    public Tag(Guid id, string code)
        : base(id)
    {
        Code = CatalogGuard.Required(code, 100, nameof(code)).ToUpperInvariant();
    }

    public string Code { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<TagTranslation> Translations => _translations;

    public void SetActive(bool isActive) => IsActive = isActive;

    public void Update(string code) =>
        Code = CatalogGuard.Required(code, 100, nameof(code)).ToUpperInvariant();

    public void AddTranslation(string languageCode, string name, string slug)
    {
        var normalizedLanguage = CatalogGuard.LanguageCode(languageCode);
        if (_translations.Any(item => item.LanguageCode == normalizedLanguage))
        {
            throw new InvalidOperationException(
                $"Translation '{normalizedLanguage}' already exists.");
        }

        _translations.Add(
            new TagTranslation(Id, normalizedLanguage, name, slug));
    }

    public void SetTranslation(
        string languageCode,
        string name,
        string slug)
    {
        var language = CatalogGuard.LanguageCode(languageCode);
        var translation = _translations.SingleOrDefault(
            item => item.LanguageCode == language);
        if (translation is null)
        {
            AddTranslation(language, name, slug);
            return;
        }

        translation.Update(name, slug);
    }

    public void RemoveTranslation(string languageCode)
    {
        var language = CatalogGuard.LanguageCode(languageCode);
        var translation = _translations.SingleOrDefault(
            item => item.LanguageCode == language);
        if (translation is not null && _translations.Count > 1)
        {
            _translations.Remove(translation);
        }
    }
}
