using Ekiphan.Domain.DataImport;

namespace Ekiphan.UnitTests.DataImport;

public sealed class ImportRowTests
{
    private const string Checksum =
        "1123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";

    [Fact]
    public void RawPayloadMustBeValidJson()
    {
        var job = CreateValidatingJob();

        Assert.Throws<ArgumentException>(
            () => job.AddRow(
                Guid.NewGuid(),
                "Products",
                2,
                "{invalid-json",
                "SKU-1"));
    }

    [Fact]
    public void RowWithErrorCannotBeMarkedValid()
    {
        var job = CreateValidatingJob();
        var row = job.AddRow(
            Guid.NewGuid(),
            "Products",
            2,
            """{"sku":null}""");
        row.AddIssue(
            Guid.NewGuid(),
            ImportIssueSeverity.Error,
            "SKU_REQUIRED",
            "SKU is required.");

        Assert.Throws<InvalidOperationException>(
            () => row.MarkValid("""{"sku":"SKU-1"}"""));
    }

    [Fact]
    public void WarningDoesNotPreventValidStatus()
    {
        var job = CreateValidatingJob();
        var row = job.AddRow(
            Guid.NewGuid(),
            "Products",
            2,
            """{"material":"çelik"}""");
        row.AddIssue(
            Guid.NewGuid(),
            ImportIssueSeverity.Warning,
            "MATERIAL_NORMALIZED",
            "Material was normalized.");

        row.MarkValid("""{"material":"STEEL"}""");

        Assert.Equal(ImportRowStatus.Valid, row.Status);
        Assert.Single(row.Issues);
    }

    private static ImportJob CreateValidatingJob()
    {
        var job = new ImportJob(
            Guid.NewGuid(),
            ImportSourceType.Excel,
            "products.xlsx",
            Checksum,
            isDryRun: true);
        job.StartValidation(DateTimeOffset.UtcNow);
        return job;
    }
}
