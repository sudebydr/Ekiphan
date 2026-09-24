namespace Ekiphan.Application.Seed;

using System.Threading;
using System.Threading.Tasks;

public interface IPerformanceTestDataSeeder
{
    Task SeedAsync(PerformanceSeedOptions options, CancellationToken cancellationToken);
}
