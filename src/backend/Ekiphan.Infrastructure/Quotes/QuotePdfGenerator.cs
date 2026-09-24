using System.Globalization;
using System.Text;
using Ekiphan.Application.Quotes;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Quotes;

public sealed class QuotePdfGenerator(EkiphanDbContext dbContext) : IQuotePdfGenerator
{
    public async Task<QuotePdfResult> GenerateAsync(
        Guid quoteId,
        CancellationToken cancellationToken = default)
    {
        var quote = await dbContext.QuoteRequests
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == quoteId, cancellationToken);

        if (quote is null)
        {
            throw new KeyNotFoundException("Quote not found.");
        }

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head><style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 30px; color: #333; }");
        html.AppendLine(".header { border-bottom: 2px solid #0056b3; padding-bottom: 10px; margin-bottom: 20px; }");
        html.AppendLine(".title { font-size: 24px; font-weight: bold; color: #0056b3; }");
        html.AppendLine(".info-table { width: 100%; margin-bottom: 20px; border-collapse: collapse; }");
        html.AppendLine(".info-table td { padding: 6px; }");
        html.AppendLine(".items-table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
        html.AppendLine(".items-table th, .items-table td { border: 1px solid #ddd; padding: 10px; text-align: left; }");
        html.AppendLine(".items-table th { background-color: #f4f6f8; }");
        html.AppendLine(".disclaimer { margin-top: 30px; font-size: 12px; color: #666; font-style: italic; }");
        html.AppendLine("</style></head><body>");

        html.AppendLine("<div class='header'>");
        html.AppendLine("<div class='title'>EKİPHAN ENDÜSTRİYEL</div>");
        html.AppendLine(CultureInfo.InvariantCulture, $"<div>Teklif Numarası: <strong>{System.Net.WebUtility.HtmlEncode(quote.RequestNumber)}</strong></div>");
        html.AppendLine(CultureInfo.InvariantCulture, $"<div>Tarih: {quote.CreatedAt:dd.MM.yyyy HH:mm}</div>");
        html.AppendLine("</div>");

        html.AppendLine("<table class='info-table'>");
        html.AppendLine(CultureInfo.InvariantCulture, $"<tr><td><strong>Müşteri / Firma:</strong> {System.Net.WebUtility.HtmlEncode(quote.FullName)} ({System.Net.WebUtility.HtmlEncode(quote.CompanyName)})</td></tr>");
        html.AppendLine(CultureInfo.InvariantCulture, $"<tr><td><strong>E-posta:</strong> {System.Net.WebUtility.HtmlEncode(quote.Email)} | <strong>Telefon:</strong> {System.Net.WebUtility.HtmlEncode(quote.Phone)}</td></tr>");
        html.AppendLine(CultureInfo.InvariantCulture, $"<tr><td><strong>Ülke / Şehir:</strong> {System.Net.WebUtility.HtmlEncode(quote.Country)} / {System.Net.WebUtility.HtmlEncode(quote.City ?? "-")}</td></tr>");
        if (!string.IsNullOrWhiteSpace(quote.ProjectName))
        {
            html.AppendLine(CultureInfo.InvariantCulture, $"<tr><td><strong>Proje Adı:</strong> {System.Net.WebUtility.HtmlEncode(quote.ProjectName)}</td></tr>");
        }
        html.AppendLine("</table>");

        html.AppendLine("<h3>Talep Edilen Ürünler</h3>");
        html.AppendLine("<table class='items-table'>");
        html.AppendLine("<thead><tr><th>#</th><th>SKU</th><th>Ürün Adı</th><th>Marka</th><th>Miktar</th></tr></thead>");
        html.AppendLine("<tbody>");

        int index = 1;
        foreach (var item in quote.Items)
        {
            html.AppendLine("<tr>");
            html.AppendLine(CultureInfo.InvariantCulture, $"<td>{index++}</td>");
            html.AppendLine(CultureInfo.InvariantCulture, $"<td>{System.Net.WebUtility.HtmlEncode(item.SKU)}</td>");
            html.AppendLine(CultureInfo.InvariantCulture, $"<td>{System.Net.WebUtility.HtmlEncode(item.ProductName)}</td>");
            html.AppendLine(CultureInfo.InvariantCulture, $"<td>{System.Net.WebUtility.HtmlEncode(item.BrandName ?? "-")}</td>");
            html.AppendLine(CultureInfo.InvariantCulture, $"<td>{item.Quantity} adet</td>");
            html.AppendLine("</tr>");
        }

        html.AppendLine("</tbody>");
        html.AppendLine("</table>");

        html.AppendLine("<div class='disclaimer'>");
        html.AppendLine("* Fiyat bilgisi ve detaylı teslimat takvimi satış ekibimiz tarafından ayrıca tarafınıza iletilecektir.");
        html.AppendLine("<br>* Bu belge Ekiphan Teklif Yönetim Sistemi tarafından üretilmiştir.");
        html.AppendLine("</div>");

        html.AppendLine("</body></html>");

        var bytes = Encoding.UTF8.GetBytes(html.ToString());
        var fileName = $"Teklif_{quote.RequestNumber}.pdf";

        return new QuotePdfResult(bytes, fileName, "application/pdf");
    }
}
