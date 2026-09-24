using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Catalog;

public sealed class ProductAttributeValue : Entity
{
    private ProductAttributeValue()
    {
    }

    private ProductAttributeValue(
        Guid id,
        Guid productId,
        Guid attributeId,
        int sequence,
        string? rawValue)
        : base(id)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product identifier cannot be empty.",
                nameof(productId));
        }

        if (attributeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Attribute identifier cannot be empty.",
                nameof(attributeId));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(sequence);

        ProductId = productId;
        AttributeId = attributeId;
        Sequence = sequence;
        RawValue = rawValue?.Trim();
    }

    public Guid ProductId { get; private set; }

    public Guid AttributeId { get; private set; }

    public int Sequence { get; private set; }

    public string? TextValue { get; private set; }

    public decimal? NumericValue { get; private set; }

    public bool? BooleanValue { get; private set; }

    public Guid? AttributeOptionId { get; private set; }

    public Guid? UnitId { get; private set; }

    public string? RawValue { get; private set; }

    public static ProductAttributeValue FromText(
        Guid id,
        Guid productId,
        Guid attributeId,
        string value,
        int sequence = 0,
        string? rawValue = null)
    {
        var item = new ProductAttributeValue(
            id,
            productId,
            attributeId,
            sequence,
            rawValue);
        item.TextValue = CatalogGuard.Required(value, 2000, nameof(value));
        return item;
    }

    public static ProductAttributeValue FromNumber(
        Guid id,
        Guid productId,
        Guid attributeId,
        decimal value,
        Guid? unitId = null,
        int sequence = 0,
        string? rawValue = null)
    {
        if (unitId == Guid.Empty)
        {
            throw new ArgumentException("Unit identifier cannot be empty.", nameof(unitId));
        }

        var item = new ProductAttributeValue(
            id,
            productId,
            attributeId,
            sequence,
            rawValue);
        item.NumericValue = value;
        item.UnitId = unitId;
        return item;
    }

    public static ProductAttributeValue FromBoolean(
        Guid id,
        Guid productId,
        Guid attributeId,
        bool value,
        int sequence = 0,
        string? rawValue = null)
    {
        var item = new ProductAttributeValue(
            id,
            productId,
            attributeId,
            sequence,
            rawValue);
        item.BooleanValue = value;
        return item;
    }

    public static ProductAttributeValue FromOption(
        Guid id,
        Guid productId,
        Guid attributeId,
        Guid optionId,
        int sequence = 0,
        string? rawValue = null)
    {
        if (optionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Option identifier cannot be empty.",
                nameof(optionId));
        }

        var item = new ProductAttributeValue(
            id,
            productId,
            attributeId,
            sequence,
            rawValue);
        item.AttributeOptionId = optionId;
        return item;
    }
}
