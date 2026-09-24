using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Catalog;

public sealed class Brand : Entity
{
    private readonly List<BrandTranslation> _translations = [];

    private Brand()
    {
    }

    public Brand(Guid id, string name, string? websiteUrl = null, int sortOrder = 0)
        : base(id)
    {
        Name = CatalogGuard.Required(name, 150, nameof(name));
        WebsiteUrl = NormalizeWebsiteUrl(websiteUrl);
        SortOrder = sortOrder;
    }

    public string Name { get; private set; } = string.Empty;

    public string? WebsiteUrl { get; private set; }

    public bool IsPublished { get; private set; }

    public int SortOrder { get; private set; }

    public IReadOnlyCollection<BrandTranslation> Translations => _translations;

    public void SetPublished(bool isPublished) => IsPublished = isPublished;

    public void Update(
        string name,
        string? websiteUrl,
        int sortOrder)
    {
        Name = CatalogGuard.Required(name, 150, nameof(name));
        WebsiteUrl = NormalizeWebsiteUrl(websiteUrl);
        SortOrder = sortOrder;
    }

    public void AddTranslation(string languageCode, string description, string slug)
    {
        var normalizedLanguage = CatalogGuard.LanguageCode(languageCode);

        if (_translations.Any(item => item.LanguageCode == normalizedLanguage))
        {
            throw new InvalidOperationException(
                $"Translation '{normalizedLanguage}' already exists.");
        }

        _translations.Add(new BrandTranslation(Id, normalizedLanguage, description, slug));
    }

    public void SetTranslation(
        string languageCode,
        string description,
        string slug)
    {
        var normalizedLanguage = CatalogGuard.LanguageCode(languageCode);
        var translation = _translations.SingleOrDefault(
            item => item.LanguageCode == normalizedLanguage);
        if (translation is null)
        {
            AddTranslation(normalizedLanguage, description, slug);
            return;
        }

        translation.Update(description, slug);
    }

    public void RemoveTranslation(string languageCode)
    {
        var normalizedLanguage = CatalogGuard.LanguageCode(languageCode);
        var translation = _translations.SingleOrDefault(
            item => item.LanguageCode == normalizedLanguage);
        if (translation is null)
        {
            return;
        }

        if (_translations.Count == 1)
        {
            throw new InvalidOperationException(
                "A brand must keep at least one translation.");
        }

        _translations.Remove(translation);
    }

    private static string? NormalizeWebsiteUrl(string? websiteUrl)
    {
        if (string.IsNullOrWhiteSpace(websiteUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(websiteUrl.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException(
                "Brand website must be an absolute HTTPS URL.",
                nameof(websiteUrl));
        }

        return uri.AbsoluteUri;
    }
}
