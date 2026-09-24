namespace Ekiphan.Domain.Content;

public sealed class MenuItemTranslation
{
    private MenuItemTranslation()
    {
    }

    internal MenuItemTranslation(
        Guid menuItemId,
        string languageCode,
        string label)
    {
        MenuItemId = menuItemId;
        LanguageCode = Language(languageCode);
        Update(label);
    }

    public Guid MenuItemId { get; private set; }

    public string LanguageCode { get; private set; } = string.Empty;

    public string Label { get; private set; } = string.Empty;

    internal void Update(string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        Label = label.Trim();
        if (Label.Length > 100)
        {
            throw new ArgumentException(
                "Menu label cannot exceed 100 characters.",
                nameof(label));
        }
    }

    private static string Language(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var language = value.Trim().ToLowerInvariant();
        if (language is not ("tr" or "en"))
        {
            throw new ArgumentException(
                "Only tr and en languages are supported.",
                nameof(value));
        }

        return language;
    }
}

