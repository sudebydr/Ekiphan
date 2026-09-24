using System.Security.Cryptography;

namespace Ekiphan.Application.Quotes;

public sealed class QuoteRequestNumberGenerator : IQuoteRequestNumberGenerator
{
    public string Generate(DateTimeOffset timestamp)
    {
        Span<byte> randomBytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(randomBytes);
        return $"Q-{timestamp:yyyyMMdd}-{Convert.ToHexString(randomBytes)}";
    }
}
