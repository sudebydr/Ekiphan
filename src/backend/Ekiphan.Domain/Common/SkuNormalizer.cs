using System.Globalization;
using System.Text;

namespace Ekiphan.Domain.Common;

public static class SkuNormalizer
{
    public static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var source = value.Trim()
            .Replace('\u0130', 'I')
            .Replace('\u0131', 'I')
            .Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(source.Length);
        foreach (var character in source)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        return builder.ToString().ToUpperInvariant();
    }
}
