using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Catalog;

public sealed class UnitDefinition : Entity
{
    private UnitDefinition()
    {
    }

    public UnitDefinition(
        Guid id,
        string code,
        string symbol,
        string dimension,
        decimal conversionFactorToBase,
        bool isBaseUnit = false)
        : base(id)
    {
        if (conversionFactorToBase <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(conversionFactorToBase),
                "Conversion factor must be greater than zero.");
        }

        if (isBaseUnit && conversionFactorToBase != 1m)
        {
            throw new ArgumentException(
                "A base unit must have a conversion factor of one.",
                nameof(conversionFactorToBase));
        }

        Code = CatalogGuard.Required(code, 50, nameof(code)).ToUpperInvariant();
        Symbol = CatalogGuard.Required(symbol, 20, nameof(symbol));
        Dimension = CatalogGuard.Required(dimension, 50, nameof(dimension))
            .ToUpperInvariant();
        ConversionFactorToBase = conversionFactorToBase;
        IsBaseUnit = isBaseUnit;
    }

    public string Code { get; private set; } = string.Empty;

    public string Symbol { get; private set; } = string.Empty;

    public string Dimension { get; private set; } = string.Empty;

    public decimal ConversionFactorToBase { get; private set; }

    public bool IsBaseUnit { get; private set; }

    public bool IsActive { get; private set; } = true;

    public decimal Normalize(decimal value) => value * ConversionFactorToBase;

    public void SetActive(bool isActive) => IsActive = isActive;

    public void Update(
        string code,
        string symbol,
        decimal conversionFactorToBase)
    {
        if (conversionFactorToBase <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(conversionFactorToBase),
                "Conversion factor must be greater than zero.");
        }

        if (IsBaseUnit && conversionFactorToBase != 1m)
        {
            throw new ArgumentException(
                "A base unit must have a conversion factor of one.",
                nameof(conversionFactorToBase));
        }

        Code = CatalogGuard.Required(code, 50, nameof(code)).ToUpperInvariant();
        Symbol = CatalogGuard.Required(symbol, 20, nameof(symbol));
        ConversionFactorToBase = conversionFactorToBase;
    }
}
