namespace Ekiphan.Api.Http;

internal sealed class ApiSecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(
            () =>
            {
                var headers = context.Response.Headers;
                headers.TryAdd("X-Content-Type-Options", "nosniff");
                headers.TryAdd("Referrer-Policy", "no-referrer");
                headers.TryAdd(
                    "Permissions-Policy",
                    "camera=(), microphone=(), geolocation=(), payment=()");
                if (!context.Request.Path.StartsWithSegments(
                        "/media/catalogs",
                        StringComparison.OrdinalIgnoreCase))
                {
                    headers.TryAdd("X-Frame-Options", "DENY");
                    headers.TryAdd(
                        "Content-Security-Policy",
                        "default-src 'none'; base-uri 'none'; " +
                        "frame-ancestors 'none'; form-action 'none'");
                }
                return Task.CompletedTask;
            });
        await next(context);
    }
}
