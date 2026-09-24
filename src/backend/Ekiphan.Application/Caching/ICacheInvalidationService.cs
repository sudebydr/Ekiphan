namespace Ekiphan.Application.Caching;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface ICacheInvalidationService
{
    Task InvalidateByTagAsync(string tag, string reason, CancellationToken cancellationToken);
    Task InvalidateByTagsAsync(IEnumerable<string> tags, string reason, CancellationToken cancellationToken);
}
