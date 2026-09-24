namespace Ekiphan.Application.Quotes;

public sealed class QuoteSubmissionException(string message)
    : Exception(message);
