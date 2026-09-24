using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Content;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Enum names match specification requirements.")]
public enum SiteSettingDataType
{
    String = 1,
    Boolean = 2,
    Integer = 3,
    Decimal = 4,
    Json = 5,
    MediaReference = 6,
    Url = 7,
    Email = 8,
    Phone = 9
}

public enum SiteSettingCategory
{
    General = 1,
    Branding = 2,
    Contact = 3,
    SocialMedia = 4,
    Footer = 5,
    Seo = 6,
    Catalog = 7,
    Quote = 8,
    Showroom = 9,
    Legal = 10
}

public static class SiteSettingKeys
{
    public static class General
    {
        public const string CompanyName = "general.companyName";
        public const string ShortName = "general.shortName";
        public const string DefaultLanguage = "general.defaultLanguage";
        public const string SupportedLanguages = "general.supportedLanguages";
        public const string TimeZone = "general.timeZone";
        public const string MaintenanceMode = "general.maintenanceMode";
        public const string MaintenanceMessageTr = "general.maintenanceMessage.tr";
        public const string MaintenanceMessageEn = "general.maintenanceMessage.en";
    }

    public static class Branding
    {
        public const string PrimaryLogo = "branding.logo.primary";
        public const string DarkLogo = "branding.logo.dark";
        public const string LightLogo = "branding.logo.light";
        public const string Favicon = "branding.favicon";
        public const string DefaultShareImage = "branding.defaultShareImage";
        public const string BrandColors = "branding.colors";
        public const string LogoAltTextTr = "branding.logoAltText.tr";
        public const string LogoAltTextEn = "branding.logoAltText.en";
    }

    public static class Contact
    {
        public const string Phone = "contact.phone";
        public const string PhoneSecondary = "contact.phoneSecondary";
        public const string Email = "contact.email";
        public const string QuoteEmail = "contact.quoteEmail";
        public const string AddressTr = "contact.address.tr";
        public const string AddressEn = "contact.address.en";
        public const string MapUrl = "contact.mapUrl";
        public const string Latitude = "contact.latitude";
        public const string Longitude = "contact.longitude";
        public const string WhatsappNumber = "contact.whatsappNumber";
        public const string WorkingHoursTr = "contact.workingHours.tr";
        public const string WorkingHoursEn = "contact.workingHours.en";
    }

    public static class SocialMedia
    {
        public const string Instagram = "social.instagram";
        public const string Linkedin = "social.linkedin";
        public const string Facebook = "social.facebook";
        public const string Youtube = "social.youtube";
        public const string Twitter = "social.twitter";
        public const string Pinterest = "social.pinterest";
        public const string Tiktok = "social.tiktok";
    }

    public static class Footer
    {
        public const string DescriptionTr = "footer.description.tr";
        public const string DescriptionEn = "footer.description.en";
        public const string CopyrightTr = "footer.copyright.tr";
        public const string CopyrightEn = "footer.copyright.en";
        public const string KvkkLink = "footer.kvkkLink";
        public const string PrivacyLink = "footer.privacyLink";
        public const string CookiePolicyLink = "footer.cookiePolicyLink";
        public const string FooterLogo = "footer.logo";
    }

    public static class Seo
    {
        public const string DefaultMetaTitle = "seo.defaultMetaTitle";
        public const string DefaultMetaDescription = "seo.defaultMetaDescription";
        public const string DefaultOgTitle = "seo.defaultOgTitle";
        public const string DefaultOgDescription = "seo.defaultOgDescription";
        public const string DefaultOgImage = "seo.defaultOgImage";
        public const string SiteName = "seo.siteName";
        public const string TitleTemplate = "seo.titleTemplate";
        public const string CanonicalBaseUrl = "seo.canonicalBaseUrl";
        public const string OrganizationSchemaJson = "seo.organizationSchemaJson";
    }

    public static class Catalog
    {
        public const string DefaultPageSize = "catalog.defaultPageSize";
        public const string MaxPageSize = "catalog.maxPageSize";
        public const string NoImagePlaceholder = "catalog.noImagePlaceholder";
        public const string ProductComparisonEnabled = "catalog.productComparisonEnabled";
        public const string QuoteListEnabled = "catalog.quoteListEnabled";
    }

    public static class Quote
    {
        public const string NotificationEmails = "quote.notificationEmails";
        public const string CustomerAutoReplyEnabled = "quote.customerAutoReplyEnabled";
        public const string ResponseTimeTextTr = "quote.responseTimeText.tr";
        public const string ResponseTimeTextEn = "quote.responseTimeText.en";
        public const string KvkkVersion = "quote.kvkkVersion";
        public const string MaxQuoteItems = "quote.maxQuoteItems";
    }
}

public sealed class SiteSetting : Entity
{
    private SiteSetting() { }

    public SiteSetting(
        Guid id,
        string key,
        SiteSettingCategory category,
        string? valueJson,
        SiteSettingDataType dataType,
        bool isPublic = true,
        bool isSensitive = false,
        Guid? updatedByUserId = null)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));

        Key = key.Trim().ToLowerInvariant();
        Category = category;
        ValueJson = valueJson;
        DataType = dataType;
        IsPublic = isPublic;
        IsSensitive = isSensitive;
        UpdatedByUserId = updatedByUserId;
        UpdatedAt = DateTimeOffset.UtcNow;
        RowVersion = Array.Empty<byte>();
    }

    public string Key { get; private set; } = string.Empty;
    public SiteSettingCategory Category { get; private set; }
    public string? ValueJson { get; private set; }
    public SiteSettingDataType DataType { get; private set; }
    public bool IsPublic { get; private set; }
    public bool IsSensitive { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public new DateTimeOffset UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public void UpdateValue(string? valueJson, Guid actorUserId)
    {        ValueJson = valueJson;
        UpdatedByUserId = actorUserId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
