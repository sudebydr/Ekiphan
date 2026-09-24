namespace Ekiphan.Application.Quotes;

public sealed record SubmitQuoteCommand(
    string FullName,
    string CompanyName,
    string Phone,
    string Email,
    string Country,
    string? City,
    string? Sector,
    string? ProjectName,
    string? Message,
    string LanguageCode,
    bool KvkkConsent,
    bool CommercialCommunicationConsent,
    string? Website,
    IReadOnlyList<SubmitQuoteItem> Items);

public sealed record SubmitQuoteItem(
    Guid? ProductId,
    Guid? VariantId,
    int Quantity,
    string? Note,
    string? ProductName = null,
    string? Sku = null,
    string? Brand = null,
    string? ImageUrl = null);

public sealed record SubmitQuoteResult(
    string RequestNumber,
    DateTimeOffset ReceivedAt);

public sealed record QuoteProductSnapshot(
    Guid ProductId,
    string ProductName,
    string SKU,
    string? BrandName,
    string? ImageStorageKey,
    IReadOnlyDictionary<Guid, QuoteVariantSnapshot> Variants);

public sealed record QuoteVariantSnapshot(
    Guid VariantId,
    string SKU,
    string Description);
