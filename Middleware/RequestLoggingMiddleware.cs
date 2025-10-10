/*
 * File Name: RequestLoggingMiddleware.cs
 * Description: Middleware for logging HTTP requests.
 */

using System.Diagnostics;

namespace EadChargingBookingBackend.Middleware;

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
        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;

        // Log incoming request
        _logger.LogInformation("Request: {Method} {Path} from {RemoteIpAddress} at {Timestamp}",
            request.Method,
            request.Path,
            context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            DateTime.UtcNow);

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request failed: {Method} {Path} - {ErrorMessage}",
                request.Method, request.Path, ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();

            // Log response
            _logger.LogInformation("Response: {StatusCode} for {Method} {Path} in {ElapsedMilliseconds}ms",
                context.Response.StatusCode,
                request.Method,
                request.Path,
                stopwatch.ElapsedMilliseconds);
        }
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}