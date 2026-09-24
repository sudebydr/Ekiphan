using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Catalog;

public sealed class AttributeDefinition : Entity
{
    private readonly List<AttributeOption> _options = [];
    private readonly List<AttributeTranslation> _translations = [];

    private AttributeDefinition()
    {
    }

    public AttributeDefinition(
        Guid id,
        string code,
        AttributeDataType dataType,
        string? unitDimension = null)
        : base(id)
    {
        if (!Enum.IsDefined(dataType))
        {
            throw new ArgumentOutOfRangeException(nameof(dataType));
        }

        if (dataType != AttributeDataType.Number &&
            !string.IsNullOrWhiteSpace(unitDimension))
        {
            throw new ArgumentException(
                "Only numeric attributes can define a unit dimension.",
                nameof(unitDimension));
        }

        Code = CatalogGuard.Required(code, 100, nameof(code)).ToUpperInvariant();
        DataType = dataType;
        UnitDimension = string.IsNullOrWhiteSpace(unitDimension)
            ? null
            : CatalogGuard.Required(unitDimension, 50, nameof(unitDimension))
                .ToUpperInvariant();
    }

    public string Code { get; private set; } = string.Empty;

    public AttributeDataType DataType { get; private set; }

    public string? UnitDimension { get; private set; }

    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<AttributeOption> Options => _options;

    public IReadOnlyCollection<AttributeTranslation> Translations => _translations;

    public void SetActive(bool isActive) => IsActive = isActive;

    public void Update(string code, string? unitDimension = null)
    {
        if (DataType != AttributeDataType.Number &&
            !string.IsNullOrWhiteSpace(unitDimension))
        {
            throw new ArgumentException(
                "Only numeric attributes can define a unit dimension.",
                nameof(unitDimension));
        }

        Code = CatalogGuard.Required(code, 100, nameof(code)).ToUpperInvariant();
        UnitDimension = string.IsNullOrWhiteSpace(unitDimension)
            ? null
            : CatalogGuard.Required(unitDimension, 50, nameof(unitDimension))
                .ToUpperInvariant();
    }

    public void AddTranslation(string languageCode, string name)
    {
        var normalizedLanguage = CatalogGuard.LanguageCode(languageCode);

        if (_translations.Any(item => item.LanguageCode == normalizedLanguage))
        {
            throw new InvalidOperationException(
                $"Translation '{normalizedLanguage}' already exists.");
        }

        _translations.Add(new AttributeTranslation(Id, normalizedLanguage, name));
    }

    public void SetTranslation(string languageCode, string name)
    {
        var language = CatalogGuard.LanguageCode(languageCode);
        var translation = _translations.SingleOrDefault(
            item => item.LanguageCode == language);
        if (translation is null)
        {
            AddTranslation(language, name);
            return;
        }

        translation.Update(name);
    }

    public void RemoveTranslation(string languageCode)
    {
        var language = CatalogGuard.LanguageCode(languageCode);
        var translation = _translations.SingleOrDefault(
            item => item.LanguageCode == language);
        if (translation is null)
        {
            return;
        }

        if (_translations.Count == 1)
        {
            throw new InvalidOperationException(
                "An attribute must keep at least one translation.");
        }

        _translations.Remove(translation);
    }

    public AttributeOption AddOption(Guid optionId, string code, int sortOrder = 0)
    {
        if (DataType is not AttributeDataType.Option and not AttributeDataType.MultiOption)
        {
            throw new InvalidOperationException(
                "Options can only be added to option-based attributes.");
        }

        var normalizedCode = CatalogGuard.Required(code, 100, nameof(code))
            .ToUpperInvariant();

        if (_options.Any(option => option.Code == normalizedCode))
        {
            throw new InvalidOperationException(
                $"Option '{normalizedCode}' already exists.");
        }

        var option = new AttributeOption(optionId, Id, normalizedCode, sortOrder);
        _options.Add(option);
        return option;
    }
}
