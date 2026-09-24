using System.Globalization;
using System.Text;
using Ekiphan.Application.Quotes;
using Ekiphan.Infrastructure.Persistence;

namespace Ekiphan.Infrastructure.Quotes;

public sealed class QuoteExportService(
    IAdminQuoteQueryService queryService) : IQuoteExportService
{
    public async Task<byte[]> ExportCsvAsync(
        AdminQuoteListQuery query,
        bool includeSensitiveData,
        CancellationToken cancellationToken = default)
    {
        var exportQuery = query with { Page = 1, PageSize = 5000 };
        var pagedResult = await queryService.GetQuotesAsync(exportQuery, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("QuoteNumber,CustomerName,CompanyName,Email,Phone,Status,AssignedUser,ProductCount,CreatedAt,LastActivityAt");

        foreach (var item in pagedResult.Items)
        {
            var email = includeSensitiveData ? item.MaskedEmail : MaskEmail(item.MaskedEmail);
            var phone = includeSensitiveData ? item.MaskedPhone : MaskPhone(item.MaskedPhone);

            sb.AppendLine(CultureInfo.InvariantCulture, $"{EscapeCsv(item.RequestNumber)},{EscapeCsv(item.FullName)},{EscapeCsv(item.CompanyName)},{EscapeCsv(email)},{EscapeCsv(phone)},{EscapeCsv(item.Status.ToString())},{EscapeCsv(item.AssignedToDisplayName ?? "Unassigned")},1,{item.CreatedAt:yyyy-MM-dd HH:mm:ss},{item.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
        }

        var preamble = Encoding.UTF8.GetPreamble();
        var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[preamble.Length + contentBytes.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(contentBytes, 0, result, preamble.Length, contentBytes.Length);
        return result;
    }

    public async Task<byte[]> ExportXlsxAsync(
        AdminQuoteListQuery query,
        bool includeSensitiveData,
        bool includeHistory,
        CancellationToken cancellationToken = default)
    {
        var exportQuery = query with { Page = 1, PageSize = 5000 };
        var pagedResult = await queryService.GetQuotesAsync(exportQuery, cancellationToken);

        var xml = new StringBuilder();
        xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        xml.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
        xml.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
        xml.AppendLine("  <Worksheet ss:Name=\"Quotes\">");
        xml.AppendLine("    <Table>");
        xml.AppendLine("      <Row>");
        xml.AppendLine("        <Cell><Data ss:Type=\"String\">QuoteNumber</Data></Cell>");
        xml.AppendLine("        <Cell><Data ss:Type=\"String\">CustomerName</Data></Cell>");
        xml.AppendLine("        <Cell><Data ss:Type=\"String\">CompanyName</Data></Cell>");
        xml.AppendLine("        <Cell><Data ss:Type=\"String\">Email</Data></Cell>");
        xml.AppendLine("        <Cell><Data ss:Type=\"String\">Phone</Data></Cell>");
        xml.AppendLine("        <Cell><Data ss:Type=\"String\">Status</Data></Cell>");
        xml.AppendLine("        <Cell><Data ss:Type=\"String\">AssignedUser</Data></Cell>");
        xml.AppendLine("        <Cell><Data ss:Type=\"String\">CreatedAt</Data></Cell>");
        xml.AppendLine("      </Row>");

        foreach (var item in pagedResult.Items)
        {
            var email = includeSensitiveData ? item.MaskedEmail : MaskEmail(item.MaskedEmail);
            var phone = includeSensitiveData ? item.MaskedPhone : MaskPhone(item.MaskedPhone);

            xml.AppendLine("      <Row>");
            xml.AppendLine(CultureInfo.InvariantCulture, $"        <Cell><Data ss:Type=\"String\">{EscapeXml(item.RequestNumber)}</Data></Cell>");
            xml.AppendLine(CultureInfo.InvariantCulture, $"        <Cell><Data ss:Type=\"String\">{EscapeXml(item.FullName)}</Data></Cell>");
            xml.AppendLine(CultureInfo.InvariantCulture, $"        <Cell><Data ss:Type=\"String\">{EscapeXml(item.CompanyName)}</Data></Cell>");
            xml.AppendLine(CultureInfo.InvariantCulture, $"        <Cell><Data ss:Type=\"String\">{EscapeXml(email)}</Data></Cell>");
            xml.AppendLine(CultureInfo.InvariantCulture, $"        <Cell><Data ss:Type=\"String\">{EscapeXml(phone)}</Data></Cell>");
            xml.AppendLine(CultureInfo.InvariantCulture, $"        <Cell><Data ss:Type=\"String\">{EscapeXml(item.Status.ToString())}</Data></Cell>");
            xml.AppendLine(CultureInfo.InvariantCulture, $"        <Cell><Data ss:Type=\"String\">{EscapeXml(item.AssignedToDisplayName ?? "Unassigned")}</Data></Cell>");
            xml.AppendLine(CultureInfo.InvariantCulture, $"        <Cell><Data ss:Type=\"String\">{item.CreatedAt:yyyy-MM-dd HH:mm:ss}</Data></Cell>");
            xml.AppendLine("      </Row>");
        }

        xml.AppendLine("    </Table>");
        xml.AppendLine("  </Worksheet>");
        xml.AppendLine("</Workbook>");

        return Encoding.UTF8.GetBytes(xml.ToString());
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        var str = value;
        if (str.StartsWith('=') || str.StartsWith('+') || str.StartsWith('-') || str.StartsWith('@'))
        {
            str = "'" + str;
        }
        return "\"" + str.Replace("\"", "\"\"") + "\"";
    }

    private static string EscapeXml(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var str = value;
        if (str.StartsWith('=') || str.StartsWith('+') || str.StartsWith('-') || str.StartsWith('@'))
        {
            str = "'" + str;
        }
        return System.Security.SecurityElement.Escape(str);
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return "***@***";
        var parts = email.Split('@');
        var name = parts[0];
        var maskedName = name.Length > 2 ? name[..2] + "***" : "***";
        return $"{maskedName}@{parts[1]}";
    }

    private static string MaskPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone) || phone.Length < 6) return "***";
        return phone[..3] + "****" + phone[^2..];
    }
}
