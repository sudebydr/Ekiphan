namespace Ekiphan.Application.Quotes;

public sealed class QuoteNotFoundException(Guid quoteId)
    : Exception($"Quote '{quoteId}' was not found.");

public sealed class QuoteConcurrencyException()
    : Exception("The quote was changed by another user.");
