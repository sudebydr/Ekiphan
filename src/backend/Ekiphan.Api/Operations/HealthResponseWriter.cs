using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ekiphan.Api.Operations;

internal static class HealthResponseWriter
{
    public static async Task WriteAsync(
        HttpContext context,
        HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Headers.CacheControl = "no-store";

        var checks = report.Entries
            .OrderBy(item => item.Key)
            .Select(item => new
            {
                name = item.Key,
                status = item.Value.Status.ToString().ToLowerInvariant(),
                durationMilliseconds = Math.Round(
                    item.Value.Duration.TotalMilliseconds,
                    2),
            })
            .ToArray();

        await context.Response.WriteAsJsonAsync(
            new
            {
                status = report.Status.ToString().ToLowerInvariant(),
                checks,
            },
            context.RequestAborted);
    }
}
