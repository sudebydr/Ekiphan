namespace Ekiphan.Application.Caching;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface IDistributedLockService
{
    Task<bool> TryAcquireLockAsync(string lockKey, TimeSpan expiration, CancellationToken cancellationToken);
    Task ReleaseLockAsync(string lockKey, CancellationToken cancellationToken);
}
