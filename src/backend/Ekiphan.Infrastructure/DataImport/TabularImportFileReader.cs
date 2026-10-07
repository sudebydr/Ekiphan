using System.Text;
using ClosedXML.Excel;
using Ekiphan.Application.DataImport;

namespace Ekiphan.Infrastructure.DataImport;

public sealed class TabularImportFileReader : ITabularImportFileReader
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".csv",
            ".xlsx"
        };

    public async Task<TabularImportDocument> ReadAsync(
        Stream content,
        string fileName,
        ImportFileReadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var safeFileName = Path.GetFileName(fileName);
        if (!string.Equals(fileName, safeFileName, StringComparison.Ordinal))
        {
            throw new ImportFileReadException("File name cannot contain a path.");
        }

        var extension = Path.GetExtension(safeFileName);
        if (!SupportedExtensions.Contains(extension))
        {
            throw new ImportFileReadException(
                "Only .csv and .xlsx import files are supported.");
        }

        var limits = ValidateOptions(options ?? new ImportFileReadOptions());
        await using var buffer = await CopyWithLimitAsync(
            content,
            limits.MaximumFileSizeBytes,
            cancellationToken);

        try
        {
            return extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
                ? ReadCsv(buffer, limits)
                : ReadWorkbook(buffer, limits);
        }
        catch (ImportFileReadException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException)
        {
            throw new ImportFileReadException(
                "The import file could not be parsed.",
                exception);
        }
    }

    private static TabularImportDocument ReadCsv(
        Stream content,
        ImportFileReadOptions limits)
    {
        using var reader = new StreamReader(
            content,
            new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false,
                throwOnInvalidBytes: true),
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);
        var text = reader.ReadToEnd();
        var separator = DetectSeparator(text);
        var records = ParseCsv(text, separator, limits);

        if (records.Count < 2)
        {
            throw new ImportFileReadException(
                "The CSV file must contain a header and at least one data row.");
        }

        var headers = ValidateHeaders(records[0], limits);
        var rows = new List<TabularImportRow>();

        for (var index = 1; index < records.Count; index++)
        {
            if (records[index].All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            EnsureRowCount(rows.Count + 1, limits);
            rows.Add(CreateRow(index + 1, headers, records[index]));
        }

        EnsureDataRows(rows);
        return new TabularImportDocument(
            [new TabularImportSheet("CSV", headers, rows)]);
    }

    private static TabularImportDocument ReadWorkbook(
        Stream content,
        ImportFileReadOptions limits)
    {
        using var workbook = new XLWorkbook(
            content,
            new LoadOptions
            {
                RecalculateAllFormulas = false
            });
        var sheets = new List<TabularImportSheet>();
        var totalRows = 0;

        foreach (var worksheet in workbook.Worksheets)
        {
            var lastRow = worksheet.LastRowUsed();
            if (lastRow is null || lastRow.RowNumber() < 2)
            {
                continue;
            }

            var headerRowNumber = FindProductHeaderRow(worksheet, lastRow.RowNumber(), limits.MaximumColumns);
            if (!headerRowNumber.HasValue)
            {
                continue;
            }

            var headerRow = worksheet.Row(headerRowNumber.Value);
            var lastHeaderCell = headerRow.LastCellUsed();
            if (lastHeaderCell is null)
            {
                continue;
            }

            if (lastHeaderCell.Address.ColumnNumber > limits.MaximumColumns)
            {
                throw new ImportFileReadException(
                    $"Column count exceeds the limit of {limits.MaximumColumns}.");
            }

            var headerValues = Enumerable.Range(1, lastHeaderCell.Address.ColumnNumber)
                .Select(column => worksheet.Cell(headerRowNumber.Value, column).GetFormattedString())
                .ToArray();
            var headers = ValidateHeaders(headerValues, limits);
            var rows = new List<TabularImportRow>();

            for (var rowNumber = headerRowNumber.Value + 1;
                 rowNumber <= lastRow.RowNumber();
                 rowNumber++)
            {
                var values = Enumerable.Range(1, headers.Length)
                    .Select(column =>
                        worksheet.Cell(rowNumber, column).GetFormattedString())
                    .ToArray();
                if (values.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                totalRows++;
                EnsureRowCount(totalRows, limits);
                ValidateCellLengths(values, limits);
                rows.Add(CreateRow(rowNumber, headers, values));
            }

            if (rows.Count > 0)
            {
                sheets.Add(new TabularImportSheet(worksheet.Name, headers, rows));
            }
        }

        if (sheets.Count == 0)
        {
            throw new ImportFileReadException(
                "The workbook does not contain a sheet with data rows.");
        }

        return new TabularImportDocument(sheets);
    }

    private static int? FindProductHeaderRow(
        IXLWorksheet worksheet,
        int lastRowNumber,
        int maximumColumns)
    {
        var scanLimit = Math.Min(lastRowNumber, 50);
        for (var rowNumber = 1; rowNumber <= scanLimit; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            var lastCell = row.LastCellUsed();
            if (lastCell is null || lastCell.Address.ColumnNumber > maximumColumns)
            {
                continue;
            }

            var headers = Enumerable.Range(1, lastCell.Address.ColumnNumber)
                .Select(column => worksheet.Cell(rowNumber, column).GetFormattedString())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();
            if (ProductImportNormalizer.IsProductHeader(headers))
            {
                return rowNumber;
            }
        }

        return null;
    }

    private static List<List<string>> ParseCsv(
        string text,
        char separator,
        ImportFileReadOptions limits)
    {
        var records = new List<List<string>>();
        var record = new List<string>();
        var field = new StringBuilder();
        var quoted = false;

        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (quoted && character == '"' &&
                index + 1 < text.Length && text[index + 1] == '"')
            {
                field.Append('"');
                index++;
            }
            else if (character == '"')
            {
                quoted = !quoted;
            }
            else if (!quoted && character == separator)
            {
                AddField(record, field, limits);
            }
            else if (!quoted && (character == '\r' || character == '\n'))
            {
                if (character == '\r' &&
                    index + 1 < text.Length && text[index + 1] == '\n')
                {
                    index++;
                }

                AddField(record, field, limits);
                records.Add(record);
                record = [];
            }
            else
            {
                field.Append(character);
                if (field.Length > limits.MaximumCellLength)
                {
                    throw new ImportFileReadException(
                        $"Cell length exceeds the limit of {limits.MaximumCellLength}.");
                }
            }
        }

        if (quoted)
        {
            throw new ImportFileReadException(
                "The CSV file contains an unterminated quoted value.");
        }

        if (field.Length > 0 || record.Count > 0)
        {
            AddField(record, field, limits);
            records.Add(record);
        }

        return records;
    }

    private static void AddField(
        List<string> record,
        StringBuilder field,
        ImportFileReadOptions limits)
    {
        if (record.Count >= limits.MaximumColumns)
        {
            throw new ImportFileReadException(
                $"Column count exceeds the limit of {limits.MaximumColumns}.");
        }

        record.Add(field.ToString().Trim());
        field.Clear();
    }

    private static string[] ValidateHeaders(
        IReadOnlyList<string> headerValues,
        ImportFileReadOptions limits)
    {
        if (headerValues.Count == 0 || headerValues.Count > limits.MaximumColumns)
        {
            throw new ImportFileReadException("The header column count is invalid.");
        }

        ValidateCellLengths(headerValues, limits);
        var headers = headerValues.Select(value => value.Trim()).ToArray();
        if (headers.Any(string.IsNullOrWhiteSpace))
        {
            throw new ImportFileReadException("Header names cannot be empty.");
        }

        if (headers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != headers.Length)
        {
            throw new ImportFileReadException("Header names must be unique.");
        }

        return headers;
    }

    private static TabularImportRow CreateRow(
        int rowNumber,
        string[] headers,
        IReadOnlyList<string> cells)
    {
        var values = new Dictionary<string, string?>(
            headers.Length,
            StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < headers.Length; index++)
        {
            var value = index < cells.Count ? cells[index].Trim() : null;
            values.Add(
                headers[index],
                string.IsNullOrWhiteSpace(value) ? null : value);
        }

        return new TabularImportRow(rowNumber, values);
    }

    private static void ValidateCellLengths(
        IEnumerable<string> values,
        ImportFileReadOptions limits)
    {
        if (values.Any(value => value.Length > limits.MaximumCellLength))
        {
            throw new ImportFileReadException(
                $"Cell length exceeds the limit of {limits.MaximumCellLength}.");
        }
    }

    private static char DetectSeparator(string text)
    {
        var firstRecord = text.Split(['\r', '\n'], 2)[0];
        var candidates = new[] { ';', ',', '\t' };
        return candidates
            .OrderByDescending(candidate => firstRecord.Count(value => value == candidate))
            .First();
    }

    private static void EnsureRowCount(
        int rowCount,
        ImportFileReadOptions limits)
    {
        if (rowCount > limits.MaximumRows)
        {
            throw new ImportFileReadException(
                $"Data row count exceeds the limit of {limits.MaximumRows}.");
        }
    }

    private static void EnsureDataRows(List<TabularImportRow> rows)
    {
        if (rows.Count == 0)
        {
            throw new ImportFileReadException(
                "The import file does not contain any data rows.");
        }
    }

    private static ImportFileReadOptions ValidateOptions(
        ImportFileReadOptions options)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            options.MaximumFileSizeBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaximumRows);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaximumColumns);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaximumCellLength);
        return options;
    }

    private static async Task<MemoryStream> CopyWithLimitAsync(
        Stream source,
        long maximumBytes,
        CancellationToken cancellationToken)
    {
        var destination = new MemoryStream();
        var buffer = new byte[81_920];
        long totalBytes = 0;

        while (true)
        {
            var bytesRead = await source.ReadAsync(
                buffer.AsMemory(),
                cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            totalBytes += bytesRead;
            if (totalBytes > maximumBytes)
            {
                await destination.DisposeAsync();
                throw new ImportFileReadException(
                    $"File size exceeds the limit of {maximumBytes} bytes.");
            }

            await destination.WriteAsync(
                buffer.AsMemory(0, bytesRead),
                cancellationToken);
        }

        destination.Position = 0;
        return destination;
    }
}
