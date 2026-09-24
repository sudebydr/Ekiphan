using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Quotes;

public sealed class QuoteRequestItem : Entity
{
    private QuoteRequestItem()
    {
    }

    internal QuoteRequestItem(
        Guid id,
        Guid quoteRequestId,
        Guid? productId,
        Guid? variantId,
        string productName,
        string sku,
        string? brandName,
        int quantity,
        string? variantSnapshot,
        string? productNote,
        string? imageStorageKey)
        : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        QuoteRequestId = quoteRequestId;
        ProductId = productId;
        VariantId = variantId;
        ProductName = QuoteGuard.Required(productName, 250, nameof(productName));
        SKU = QuoteGuard.Required(sku, 100, nameof(sku)).ToUpperInvariant();
        BrandName = QuoteGuard.Optional(brandName, 150, nameof(brandName));
        Quantity = quantity;
        VariantSnapshot = QuoteGuard.Optional(
            variantSnapshot,
            1000,
            nameof(variantSnapshot));
        ProductNote = QuoteGuard.Optional(productNote, 2000, nameof(productNote));
        ImageStorageKey = ValidateStorageKey(imageStorageKey);
    }

    public Guid QuoteRequestId { get; private set; }

    public Guid? ProductId { get; private set; }

    public Guid? VariantId { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public string SKU { get; private set; } = string.Empty;

    public string? BrandName { get; private set; }

    public int Quantity { get; private set; }

    public string? VariantSnapshot { get; private set; }

    public string? ProductNote { get; private set; }

    public string? ImageStorageKey { get; private set; }

    public void ChangeQuantity(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);
        Quantity = quantity;
    }

    private static string? ValidateStorageKey(string? storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return null;
        }

        var normalized = QuoteGuard.Required(
            storageKey,
            500,
            nameof(storageKey));

        if (normalized.StartsWith('/') ||
            normalized.Contains('\\') ||
            normalized.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Image storage key must be a safe relative path.",
                nameof(storageKey));
        }

        return normalized;
    }
}
