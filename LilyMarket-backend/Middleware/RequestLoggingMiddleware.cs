using System.Diagnostics;

namespace LilyMarket.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        await _next(context);

        sw.Stop();

        var userId = context.User?.FindFirst("sub")?.Value ?? "anonymous";

        _logger.LogInformation(
            "{Method} {Path} | {StatusCode} | {UserId} | {ElapsedMs}ms",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            userId,
            sw.ElapsedMilliseconds);
    }
}