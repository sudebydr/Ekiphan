namespace Ekiphan.Domain.Catalog;

public static class LanguageCodes
{
    public const string Turkish = "tr";
    public const string English = "en";

    public static bool IsSupported(string languageCode) =>
        languageCode is Turkish or English;
}
