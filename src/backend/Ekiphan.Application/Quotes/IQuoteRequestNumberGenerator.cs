namespace Ekiphan.Application.Quotes;

public interface IQuoteRequestNumberGenerator
{
    string Generate(DateTimeOffset timestamp);
}
