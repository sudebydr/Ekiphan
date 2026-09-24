using System.Text;
using Ekiphan.Domain.DataImport;

namespace Ekiphan.Application.DataImport;

public sealed class ImportIssueCsvWriter(IImportIssueReportSource source)
    : IImportIssueReportWriter
{
    private static readonly string[] Headers =
    [
        "SheetName",
        "RowNumber",
        "SKU",
        "Severity",
        "Code",
        "Message",
        "ColumnName",
        "RawValue",
    ];

    public async Task WriteCsvAsync(
        Guid jobId,
        Stream destination,
        ImportIssueSeverity? severity = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        await using var writer = new StreamWriter(
            destination,
            new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: true,
                throwOnInvalidBytes: true),
            bufferSize: 16_384,
            leaveOpen: true);
        await WriteRecordAsync(writer, Headers, cancellationToken);

        await foreach (var issue in source
            .StreamAsync(jobId, severity, cancellationToken)
            .WithCancellation(cancellationToken))
        {
            await WriteRecordAsync(
                writer,
                [
                    issue.SheetName,
                    issue.RowNumber.ToString(
                        System.Globalization.CultureInfo.InvariantCulture),
                    issue.SKU ?? string.Empty,
                    issue.Severity.ToString(),
                    issue.Code,
                    issue.Message,
                    issue.ColumnName ?? string.Empty,
                    issue.RawValue ?? string.Empty,
                ],
                cancellationToken);
        }

        await writer.FlushAsync(cancellationToken);
    }

    private static async Task WriteRecordAsync(
        TextWriter writer,
        string[] fields,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < fields.Length; index++)
        {
            if (index > 0)
            {
                await writer.WriteAsync(",".AsMemory(), cancellationToken);
            }

            var encoded = EncodeField(fields[index]);
            await writer.WriteAsync(encoded.AsMemory(), cancellationToken);
        }

        await writer.WriteLineAsync(ReadOnlyMemory<char>.Empty, cancellationToken);
    }

    public static string EncodeField(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var safeValue = IsFormulaCandidate(value) ? $"'{value}" : value;
        return $"\"{safeValue.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static bool IsFormulaCandidate(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        var index = 0;
        while (index < value.Length && value[index] == ' ')
        {
            index++;
        }

        if (index == value.Length)
        {
            return false;
        }

        var firstNonSpace = value[index];
        return firstNonSpace is '=' or '+' or '-' or '@' or '\t' or '\r';
    }
}
