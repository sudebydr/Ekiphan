namespace Ekiphan.Application.Warmup;

using System.Threading;
using System.Threading.Tasks;
using Ekiphan.Application.Warmup.Models;

public interface ICacheWarmupService
{
    Task WarmupAsync(CacheWarmupRequest request, CancellationToken cancellationToken);
}
