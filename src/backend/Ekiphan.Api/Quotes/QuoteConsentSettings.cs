namespace Ekiphan.Api.Quotes;

internal sealed class QuoteConsentSettings
{
    public const string SectionName = "QuoteConsent";

    public string? KvkkVersion { get; init; }

    public string? CommercialCommunicationVersion { get; init; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(KvkkVersion) &&
        KvkkVersion.Length <= 100 &&
        !string.IsNullOrWhiteSpace(CommercialCommunicationVersion) &&
        CommercialCommunicationVersion.Length <= 100;
}
