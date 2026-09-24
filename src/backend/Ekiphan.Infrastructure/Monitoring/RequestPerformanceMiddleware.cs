namespace Ekiphan.Infrastructure.Monitoring;

using System.Diagnostics;
using System.Threading.Tasks;
using Ekiphan.Application.Monitoring;
using Microsoft.AspNetCore.Http;

public class RequestPerformanceMiddleware
{
    private readonly RequestDelegate _next;

    public RequestPerformanceMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRequestPerformanceMonitor monitor)
    {
        var sw = Stopwatch.StartNew();
        
        await _next(context);
        
        sw.Stop();
        
        // Log the performance details
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? string.Empty;
        var statusCode = context.Response.StatusCode;
        var userId = context.User?.Identity?.Name;
        
        await monitor.RecordRequestAsync(
            method,
            path,
            statusCode,
            sw.Elapsed.TotalMilliseconds,
            userId,
            context.TraceIdentifier,
            false,
            0,
            context.Response.ContentLength ?? 0
        );
    }
}
