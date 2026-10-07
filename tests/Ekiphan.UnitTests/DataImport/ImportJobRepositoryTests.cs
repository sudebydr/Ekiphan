using Ekiphan.Domain.DataImport;
using Ekiphan.Infrastructure.DataImport;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.UnitTests.DataImport;

public sealed class ImportJobRepositoryTests
{
    [Fact]
    public async Task SourceExistsIgnoresDryRunsAndValidationFailuresButDetectsStagedRealImports()
    {
        await using var db = new EkiphanDbContext(new DbContextOptionsBuilder<EkiphanDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
        const string checksum = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        db.ImportJobs.Add(new ImportJob(Guid.NewGuid(), ImportSourceType.Excel, "dry.xlsx", checksum, true));
        await db.SaveChangesAsync();
        var repository = new ImportJobRepository(db);

        Assert.False(await repository.SourceExistsAsync(checksum));

        var failedJob = new ImportJob(Guid.NewGuid(), ImportSourceType.Excel, "failed.xlsx", checksum, false);
        failedJob.StartValidation(DateTimeOffset.UtcNow);
        var failedRow = failedJob.AddRow(Guid.NewGuid(), "Products", 2, "{}", "FAILED");
        failedRow.AddIssue(Guid.NewGuid(), ImportIssueSeverity.Error, "TEST", "Validation failed.");
        failedJob.CompleteValidation(DateTimeOffset.UtcNow);
        db.ImportJobs.Add(failedJob);
        await db.SaveChangesAsync();
        Assert.False(await repository.SourceExistsAsync(checksum));

        var stagedJob = new ImportJob(Guid.NewGuid(), ImportSourceType.Excel, "real.xlsx", checksum, false);
        stagedJob.StartValidation(DateTimeOffset.UtcNow);
        var stagedRow = stagedJob.AddRow(Guid.NewGuid(), "Products", 2, "{}", "REAL");
        stagedRow.MarkValid("{}");
        stagedJob.CompleteValidation(DateTimeOffset.UtcNow);
        db.ImportJobs.Add(stagedJob);
        await db.SaveChangesAsync();
        Assert.True(await repository.SourceExistsAsync(checksum));
    }
}
