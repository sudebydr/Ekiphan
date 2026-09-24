namespace Ekiphan.Domain.Seo;

public enum SeoEntityType
{
    Product = 1,
    Category = 2,
    Brand = 3,
    ContentPage = 4,
    NewsArticle = 5,
    ReferenceProject = 6,
    Showroom = 7,
    Gallery = 8,
    Homepage = 9,
    StaticPage = 10
}

public enum RedirectType
{
    Permanent301 = 301,
    Temporary302 = 302,
    Permanent308 = 308,
    Temporary307 = 307
}

public enum RedirectMatchType
{
    Exact = 1,
    Prefix = 2
}

public enum RedirectRuleSourceType
{
    Manual = 1,
    SlugChange = 2,
    Import = 3,
    System = 4
}

public enum BrokenLinkScanStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    PartiallyCompleted = 4,
    Failed = 5,
    Cancelled = 6
}

public enum BrokenLinkResultStatus
{
    Valid = 1,
    Redirected = 2,
    Broken = 3,
    Timeout = 4,
    Blocked = 5,
    InvalidUrl = 6,
    Skipped = 7
}

public enum LinkType
{
    Internal = 1,
    External = 2,
    Media = 3,
    Anchor = 4
}

public enum SeoChangeSource
{
    Manual = 1,
    Import = 2,
    SlugChange = 3,
    System = 4,
    Rollback = 5
}
