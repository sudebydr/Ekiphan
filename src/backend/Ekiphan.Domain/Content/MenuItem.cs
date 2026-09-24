using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Content;

public sealed class MenuItem : Entity
{
    private readonly List<MenuItemTranslation> _translations = [];

    private MenuItem()
    {
    }

    public MenuItem(
        Guid id,
        string code,
        MenuLocation location,
        string url,
        bool isExternal,
        bool openInNewTab,
        Guid? parentId = null,
        int sortOrder = 0)
        : base(id)
    {
        Configure(
            code,
            location,
            url,
            isExternal,
            openInNewTab,
            parentId,
            sortOrder);
    }

    public string Code { get; private set; } = string.Empty;

    public MenuLocation Location { get; private set; }

    public string Url { get; private set; } = string.Empty;

    public bool IsExternal { get; private set; }

    public bool OpenInNewTab { get; private set; }

    public Guid? ParentId { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsPublished { get; private set; }

    public IReadOnlyCollection<MenuItemTranslation> Translations =>
        _translations;

    public void Configure(
        string code,
        MenuLocation location,
        string url,
        bool isExternal,
        bool openInNewTab,
        Guid? parentId,
        int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var normalizedCode = code.Trim().ToUpperInvariant();
        if (normalizedCode.Length > 100 ||
            normalizedCode.Any(item =>
                !char.IsAsciiLetterOrDigit(item) && item != '_'))
        {
            throw new ArgumentException(
                "Code must contain up to 100 ASCII letters, digits or underscores.",
                nameof(code));
        }

        if (!Enum.IsDefined(location))
        {
            throw new ArgumentOutOfRangeException(nameof(location));
        }

        if (parentId == Id)
        {
            throw new ArgumentException(
                "A menu item cannot be its own parent.",
                nameof(parentId));
        }

        Code = normalizedCode;
        Location = location;
        Url = ValidateUrl(url, isExternal);
        IsExternal = isExternal;
        OpenInNewTab = isExternal || openInNewTab;
        ParentId = parentId;
        SortOrder = sortOrder;
    }

    public void SetPublished(bool isPublished)
    {
        if (isPublished && _translations.Count == 0)
        {
            throw new InvalidOperationException(
                "A menu item cannot be published without a translation.");
        }

        IsPublished = isPublished;
    }

    public void SetTranslation(
        string languageCode,
        string label)
    {
        var language = languageCode.Trim().ToLowerInvariant();
        var translation = _translations.SingleOrDefault(
            item => item.LanguageCode == language);
        if (translation is null)
        {
            _translations.Add(new MenuItemTranslation(Id, language, label));
            return;
        }

        translation.Update(label);
    }

    public void RemoveTranslation(string languageCode)
    {
        var language = languageCode.Trim().ToLowerInvariant();
        var translation = _translations.SingleOrDefault(
            item => item.LanguageCode == language);
        if (translation is not null && _translations.Count > 1)
        {
            _translations.Remove(translation);
        }
    }

    private static string ValidateUrl(string value, bool isExternal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var url = value.Trim();
        if (url.Length > 2048)
        {
            throw new ArgumentException(
                "Menu URL cannot exceed 2048 characters.",
                nameof(value));
        }

        if (!isExternal &&
            ((url.StartsWith('/') && !url.StartsWith("//", StringComparison.Ordinal)) ||
             (url.StartsWith('#') && url.Length > 1)))
        {
            return url;
        }

        if (isExternal &&
            Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            uri.Scheme == Uri.UriSchemeHttps &&
            string.IsNullOrEmpty(uri.UserInfo))
        {
            return url;
        }

        throw new ArgumentException(
            isExternal
                ? "External menu URL must be absolute HTTPS."
                : "Internal menu URL must be site-relative or an anchor.",
            nameof(value));
    }
}

