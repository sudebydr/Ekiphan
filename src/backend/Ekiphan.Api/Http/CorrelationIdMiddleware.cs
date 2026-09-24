using System.Diagnostics;

namespace Ekiphan.Api.Http;

internal sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetCorrelationId(context);
        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(
            () =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

        using (logger.BeginScope(
            new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
            }))
        {
            await next(context);
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        var supplied = context.Request.Headers[HeaderName].ToString();
        if (IsValid(supplied))
        {
            return supplied;
        }

        var traceId = Activity.Current?.TraceId.ToString();
        return string.IsNullOrWhiteSpace(traceId)
            ? Guid.NewGuid().ToString("N")
            : traceId;
    }

    private static bool IsValid(string value) =>
        value.Length is >= 8 and <= 64 &&
        value.All(character =>
            char.IsAsciiLetterOrDigit(character) ||
            character is '-' or '_');
}
