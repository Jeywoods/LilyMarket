using System.Text.Json;
using LilyMarket.Domain.Exceptions;

namespace LilyMarket.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BidValidationException ex)
        {
            _logger.LogWarning("Bid validation failed: {Code} - {Message}", ex.Code, ex.Message);
            await WriteProblemDetails(context, 400, ex.Code, ex.Message);
        }
        catch (AuctionNotFoundException ex)
        {
            _logger.LogWarning("Auction not found: {Message}", ex.Message);
            await WriteProblemDetails(context, 404, ex.Code, ex.Message);
        }
        catch (UnauthorizedOperationException ex)
        {
            _logger.LogWarning("Unauthorized: {Message}", ex.Message);
            await WriteProblemDetails(context, 403, ex.Code, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized access: {Message}", ex.Message);
            await WriteProblemDetails(context, 401, "UNAUTHORIZED", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid operation: {Message}", ex.Message);
            await WriteProblemDetails(context, 400, "INVALID_OPERATION", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblemDetails(context, 500, "INTERNAL_ERROR", "An unexpected error occurred");
        }
    }

    private static async Task WriteProblemDetails(HttpContext context, int statusCode, string code, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = $"https://lilymarket.ru/errors/{code.ToLower()}",
            title = detail,
            status = statusCode,
            code = code,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}