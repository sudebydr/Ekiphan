using Ekiphan.Domain.DataImport;

namespace Ekiphan.UnitTests.DataImport;

public sealed class ImportJobTests
{
    private const string Checksum =
        "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";

    [Fact]
    public void OriginalFileNameCannotContainAPath()
    {
        Assert.Throws<ArgumentException>(
            () => new ImportJob(
                Guid.NewGuid(),
                ImportSourceType.Excel,
                "../products.xlsx",
                Checksum,
                isDryRun: true));
    }

    [Fact]
    public void RowsCanOnlyBeAddedDuringValidation()
    {
        var job = CreateJob();

        Assert.Throws<InvalidOperationException>(
            () => job.AddRow(
                Guid.NewGuid(),
                "Products",
                2,
                """{"sku":"SKU-1"}""",
                "SKU-1"));
    }

    [Fact]
    public void DuplicateSheetAndRowNumberIsRejected()
    {
        var job = CreateJob();
        job.StartValidation(DateTimeOffset.UtcNow);
        job.AddRow(
            Guid.NewGuid(),
            "Products",
            2,
            """{"sku":"SKU-1"}""",
            "SKU-1");

        Assert.Throws<InvalidOperationException>(
            () => job.AddRow(
                Guid.NewGuid(),
                "products",
                2,
                """{"sku":"SKU-2"}""",
                "SKU-2"));
    }

    [Fact]
    public void ValidationCannotCompleteWithPendingRows()
    {
        var job = CreateJob();
        job.StartValidation(DateTimeOffset.UtcNow);
        job.AddRow(
            Guid.NewGuid(),
            "Products",
            2,
            """{"sku":"SKU-1"}""",
            "SKU-1");

        Assert.Throws<InvalidOperationException>(
            () => job.CompleteValidation(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ValidationErrorsProduceValidationFailedStatus()
    {
        var job = CreateJob();
        job.StartValidation(DateTimeOffset.UtcNow);
        var row = job.AddRow(
            Guid.NewGuid(),
            "Products",
            2,
            """{"sku":null}""");
        row.AddIssue(
            Guid.NewGuid(),
            ImportIssueSeverity.Error,
            "SKU_REQUIRED",
            "SKU is required.",
            "ürün kodu");

        job.CompleteValidation(DateTimeOffset.UtcNow);

        Assert.Equal(ImportJobStatus.ValidationFailed, job.Status);
        Assert.Equal(1, job.InvalidRowCount);
    }

    [Fact]
    public void SuccessfulDryRunCompletesWithoutPublishing()
    {
        var job = CreateJob(isDryRun: true);
        job.StartValidation(DateTimeOffset.UtcNow);
        var row = job.AddRow(
            Guid.NewGuid(),
            "Products",
            2,
            """{"sku":"SKU-1"}""",
            "SKU-1");
        row.MarkValid("""{"sku":"SKU-1","name":"Product"}""");

        job.CompleteValidation(DateTimeOffset.UtcNow);

        Assert.Equal(ImportJobStatus.Completed, job.Status);
        Assert.Null(job.PublishingStartedAt);
    }

    [Fact]
    public void PublishFlowRequiresEveryValidRowToBeProcessed()
    {
        var job = CreateJob(isDryRun: false);
        job.StartValidation(DateTimeOffset.UtcNow);
        var row = job.AddRow(
            Guid.NewGuid(),
            "Products",
            2,
            """{"sku":"SKU-1"}""",
            "SKU-1");
        row.MarkValid("""{"sku":"SKU-1","name":"Product"}""");
        job.CompleteValidation(DateTimeOffset.UtcNow);
        job.StartPublishing(DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(
            () => job.CompletePublishing(DateTimeOffset.UtcNow));

        row.MarkPublished();
        job.CompletePublishing(DateTimeOffset.UtcNow);

        Assert.Equal(ImportJobStatus.Completed, job.Status);
        Assert.Equal(1, job.PublishedRowCount);
    }

    private static ImportJob CreateJob(bool isDryRun = true) =>
        new(
            Guid.NewGuid(),
            ImportSourceType.Excel,
            "products.xlsx",
            Checksum,
            isDryRun);
}
