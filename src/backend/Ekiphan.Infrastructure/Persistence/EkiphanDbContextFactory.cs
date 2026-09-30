using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ekiphan.Infrastructure.Persistence;

public sealed class EkiphanDbContextFactory
    : IDesignTimeDbContextFactory<EkiphanDbContext>
{
    public EkiphanDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            "ConnectionStrings__EkiphanDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings__EkiphanDatabase environment variable must be set " +
                "to create EkiphanDbContext for design-time operations.");
        }

        var options = new DbContextOptionsBuilder<EkiphanDbContext>()
            .UseSqlServer(
                connectionString,
                sqlOptions => sqlOptions.MigrationsAssembly(
                    AssemblyReference.Assembly.FullName))
            .Options;

        return new EkiphanDbContext(options);
    }
}
