using Ekiphan.Application.Seo;
using Microsoft.AspNetCore.Http;

namespace Ekiphan.Infrastructure.Seo;

public sealed class SeoRedirectMiddleware
{
    private readonly RequestDelegate _next;

    public SeoRedirectMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRedirectResolver resolver)
    {
        var path = context.Request.Path.Value;

        if (!string.IsNullOrEmpty(path) &&
            !path.StartsWith("/api", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("/_next", StringComparison.OrdinalIgnoreCase))
        {
            var rule = await resolver.ResolveAsync(path, context.Request.QueryString.Value, context.RequestAborted);
            if (rule != null)
            {
                var destination = rule.DestinationUrl;
                if (rule.PreserveQueryString && context.Request.QueryString.HasValue)
                {
                    destination = destination.Contains('?')
                        ? $"{destination}&{context.Request.QueryString.Value![1..]}"
                        : $"{destination}{context.Request.QueryString.Value}";
                }

                context.Response.StatusCode = (int)rule.RedirectType;
                context.Response.Headers.Location = destination;
                return;
            }
        }

        await _next(context);
    }
}
