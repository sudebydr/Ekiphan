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
            connectionString =
                "Server=.\\SQLEXPRESS;Database=EkiphanDevelopment;" +
                "Trusted_Connection=True;TrustServerCertificate=True;" +
                "Encrypt=False";
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
