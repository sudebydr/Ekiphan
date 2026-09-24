namespace Ekiphan.Application.FeatureFlags;

using System.Threading.Tasks;

public interface IFeatureFlagService
{
    Task<bool> IsFeatureEnabledAsync(string featureName);
    Task SetFeatureFlagAsync(string featureName, bool isEnabled, string reason, string? rowVersion);
}
