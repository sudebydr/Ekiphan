using System.Text;
using Ekiphan.Application.DataImport;
using Ekiphan.Domain.DataImport;

namespace Ekiphan.UnitTests.DataImport;

public sealed class ImportIssueCsvWriterTests
{
    [Fact]
    public async Task WriteCsvAsyncWritesBomAndNeutralizesFormulaCandidates()
    {
        var source = new StubSource(
            new ImportIssueItem(
                Guid.NewGuid(),
                "=DangerousSheet",
                2,
                " +SUM(A1:A2)",
                ImportIssueSeverity.Error,
                "INVALID",
                "Value contains \"quotes\"\nand a newline.",
                "Name",
                "\t=CMD()"));
        var writer = new ImportIssueCsvWriter(source);
        await using var destination = new MemoryStream();

        await writer.WriteCsvAsync(Guid.NewGuid(), destination);

        var bytes = destination.ToArray();
        Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);
        var csv = Encoding.UTF8.GetString(bytes);
        Assert.Contains("\"'=DangerousSheet\"", csv);
        Assert.Contains("\"' +SUM(A1:A2)\"", csv);
        Assert.Contains("\"Value contains \"\"quotes\"\"", csv);
        Assert.Contains("\"'\t=CMD()\"", csv);
    }

    [Theory]
    [InlineData("=1+1", "\"'=1+1\"")]
    [InlineData("+1", "\"'+1\"")]
    [InlineData("-1", "\"'-1\"")]
    [InlineData("@name", "\"'@name\"")]
    [InlineData("safe", "\"safe\"")]
    [InlineData("a\"b", "\"a\"\"b\"")]
    [InlineData("   ", "\"   \"")]
    public void EncodeFieldProducesSafeQuotedCsv(string value, string expected)
    {
        Assert.Equal(expected, ImportIssueCsvWriter.EncodeField(value));
    }

    private sealed class StubSource(params ImportIssueItem[] items)
        : IImportIssueReportSource
    {
        public async IAsyncEnumerable<ImportIssueItem> StreamAsync(
            Guid jobId,
            ImportIssueSeverity? severity = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation]
            CancellationToken cancellationToken = default)
        {
            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
            }

            await Task.CompletedTask;
        }
    }
}
