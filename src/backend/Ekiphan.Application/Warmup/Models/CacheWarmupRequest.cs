namespace Ekiphan.Application.Warmup.Models;

public class CacheWarmupRequest
{
    public bool WarmupCategoryTree { get; set; } = true;
    public bool WarmupBrandList { get; set; } = true;
    public bool WarmupPublicSettings { get; set; } = true;
    public bool WarmupHomepageBanners { get; set; } = true;
    public bool WarmupPopularProducts { get; set; } = true;
    public bool WarmupSitemapIndex { get; set; } = true;
}
